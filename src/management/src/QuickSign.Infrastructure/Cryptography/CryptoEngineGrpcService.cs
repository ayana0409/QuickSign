using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Shared;
using QuickSign.Infrastructure.Cryptography.Protos;

namespace QuickSign.Infrastructure.Cryptography;

/// <summary>
/// Client gateway communicating with Go Crypto Engine over gRPC.
/// Implements resilient Polly retry and Compensating Transaction rollback per ADR 0001.
/// </summary>
public sealed class CryptoEngineGrpcService : ICryptoEngineGrpcService
{
    private readonly CryptoEngineService.CryptoEngineServiceClient _client;
    private readonly ILogger<CryptoEngineGrpcService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public CryptoEngineGrpcService(
        IConfiguration configuration,
        ILogger<CryptoEngineGrpcService> logger)
    {
        _logger = logger;

        var endpoint = configuration["CryptoEngine:GrpcEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Configuration 'CryptoEngine:GrpcEndpoint' is required and cannot be empty.");
        }
        var channel = GrpcChannel.ForAddress(endpoint);
        _client = new CryptoEngineService.CryptoEngineServiceClient(channel);

        // Configure Polly retry for transient network glitches
        _retryPolicy = Policy
            .Handle<RpcException>(ex => ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.DeadlineExceeded)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)),
                (exception, timeSpan, attempt, context) =>
                {
                    _logger.LogWarning("gRPC call to Go Crypto Engine failed. Attempt {Attempt} after {Delay}ms. Exception: {Message}",
                        attempt, timeSpan.TotalMilliseconds, exception.Message);
                });
    }

    public async Task<GeneratedKeyMaterialDto> GenerateCertificateKeyPairAsync(
        Guid certificateId,
        string userId,
        string subjectName,
        string algorithm,
        DateTimeOffset validFrom,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var request = new GenerateKeyRequest
        {
            CertificateId = certificateId.ToString(),
            UserId = userId,
            SubjectName = subjectName,
            Algorithm = algorithm,
            ValidFromUnix = validFrom.ToUnixTimeSeconds(),
            ExpiresAtUnix = expiresAt.ToUnixTimeSeconds()
        };

        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _client.GenerateCertificateKeyPairAsync(request, cancellationToken: cancellationToken));

            if (!response.Success)
            {
                _logger.LogError("Go Crypto Engine failed to generate key pair: {Error}", response.ErrorMessage);
                throw new BusinessException(
                    QuickSignDomainErrorCodes.CertKeygenFailed,
                    $"Failed to generate cryptographic key pair: {response.ErrorMessage}",
                    500);
            }

            return new GeneratedKeyMaterialDto
            {
                SerialNumber = response.SerialNumber,
                PublicKeyPem = response.PublicKeyPem,
                VaultKeyPath = response.VaultKeyPath
            };
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC transport failure calling Go Crypto Engine GenerateCertificateKeyPair.");
            throw new BusinessException(
                QuickSignDomainErrorCodes.ServiceUnavailable,
                "Cryptographic signing engine is temporarily unavailable.",
                503,
                innerException: ex);
        }
    }

    public async Task<bool> DeleteCertificateKeyPairAsync(
        string vaultKeyPath,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteKeyRequest
        {
            VaultKeyPath = vaultKeyPath
        };

        try
        {
            _logger.LogInformation("Triggering Compensating Transaction: Deleting orphaned key at {VaultPath} from Vault.", vaultKeyPath);

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _client.DeleteCertificateKeyPairAsync(request, cancellationToken: cancellationToken));

            if (!response.Success)
            {
                _logger.LogCritical("Compensating Transaction FAILED: Unable to delete key at {VaultPath}. Message: {Message}",
                    vaultKeyPath, response.Message);
                return false;
            }

            _logger.LogInformation("Compensating Transaction SUCCESS: Key at {VaultPath} deleted from Vault.", vaultKeyPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Compensating Transaction FAILED with exception for Vault path {VaultPath}.", vaultKeyPath);
            return false;
        }
    }
}
