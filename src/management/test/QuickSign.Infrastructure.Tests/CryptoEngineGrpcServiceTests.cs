using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Shared;
using QuickSign.Infrastructure.Cryptography;
using Xunit;

namespace QuickSign.Infrastructure.Tests;

/// <summary>
/// Integration and resilience tests for CryptoEngineGrpcService.
/// Validates gRPC client transport, Polly retry policies, error mapping, and live communication with Go Crypto Engine.
/// </summary>
public sealed class CryptoEngineGrpcServiceTests
{
    private readonly Mock<ILogger<CryptoEngineGrpcService>> _loggerMock = new();

    [Fact]
    public async Task GenerateCertificateKeyPairAsync_WhenServerUnavailable_RetriesAndThrowsServiceUnavailableException()
    {
        // Arrange: Point to a dead/unused port to trigger transport failure and Polly retry
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["CryptoEngine:GrpcEndpoint"] = "http://127.0.0.1:59998"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new CryptoEngineGrpcService(configuration, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.GenerateCertificateKeyPairAsync(
            certificateId: Guid.NewGuid(),
            userId: "test-user-resilience",
            subjectName: "Resilience Test",
            algorithm: "RSA2048",
            validFrom: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddDays(30)
        );

        // Assert: Must map RpcException to BusinessException with ServiceUnavailable and HTTP 503
        var exception = await act.Should().ThrowAsync<BusinessException>();
        exception.Which.ErrorCode.Should().Be(QuickSignDomainErrorCodes.ServiceUnavailable);
        exception.Which.HttpStatus.Should().Be(503);
    }

    [Fact]
    public async Task DeleteCertificateKeyPairAsync_WhenServerUnavailable_ReturnsFalseWithoutThrowing()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["CryptoEngine:GrpcEndpoint"] = "http://127.0.0.1:59998"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new CryptoEngineGrpcService(configuration, _loggerMock.Object);

        // Act: Compensating transaction should catch exception, log critical, and return false
        var result = await service.DeleteCertificateKeyPairAsync("quicksign-test/users/test-user/keys/orphan-key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenGrpcEndpointConfigurationMissing_ThrowsInvalidOperationException()
    {
        // Arrange: Empty configuration without CryptoEngine:GrpcEndpoint
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        Action act = () => _ = new CryptoEngineGrpcService(configuration, _loggerMock.Object);

        // Assert: Fail-Fast check on startup
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CryptoEngine:GrpcEndpoint*");
    }

    [Fact]
    public async Task LiveGoServer_WhenRunning_GeneratesKeyAndDeletesKeySuccessfully()
    {
        // Check if Go Crypto Engine is listening on default localhost:50051
        const string host = "127.0.0.1";
        const int port = 50051;
        bool isServerRunning;

        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(host, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(500));
            isServerRunning = completedTask == connectTask && tcpClient.Connected;
        }
        catch
        {
            isServerRunning = false;
        }

        if (!isServerRunning)
        {
            // Gracefully pass with note when live Go daemon is not started during unit test run
            return;
        }

        // Arrange live gRPC client
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["CryptoEngine:GrpcEndpoint"] = $"http://{host}:{port}"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new CryptoEngineGrpcService(configuration, _loggerMock.Object);
        var certId = Guid.NewGuid();
        const string userId = "test-dotnet-go-integration";

        // Act 1: Generate certificate key pair over gRPC
        var keyMaterial = await service.GenerateCertificateKeyPairAsync(
            certificateId: certId,
            userId: userId,
            subjectName: "QuickSign .NET to Go Integration Test",
            algorithm: "RSA2048",
            validFrom: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddDays(60)
        );

        // Assert 1
        keyMaterial.Should().NotBeNull();
        keyMaterial.SerialNumber.Should().NotBeNullOrWhiteSpace();
        keyMaterial.PublicKeyPem.Should().Contain("-----BEGIN CERTIFICATE-----");
        keyMaterial.VaultKeyPath.Should().NotBeNullOrWhiteSpace();

        // Act 2: Execute compensating transaction (Delete key) over gRPC
        var deleteResult = await service.DeleteCertificateKeyPairAsync(keyMaterial.VaultKeyPath);

        // Assert 2
        deleteResult.Should().BeTrue("Compensating transaction must purge the key and return true");
    }
}
