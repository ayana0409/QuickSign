using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Shared;
using QuickSign.HttpApi.Host.Middlewares;
using Xunit;

namespace QuickSign.Application.Tests.Middlewares;

public class BusinessExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<BusinessExceptionHandlingMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_WhenBusinessExceptionThrown_ShouldReturnStandardizedJsonFormat()
    {
        // Arrange: TC-MID-01
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var details = new Dictionary<string, object> { { "field", "userId" } };
        var businessException = new BusinessException(
            QuickSignDomainErrorCodes.MaxActiveCertsReached,
            "User has already reached the maximum limit of 5 active certificates.",
            400,
            details
        );

        RequestDelegate next = _ => throw businessException;
        var middleware = new BusinessExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/json");

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBodyStream);
        var responseText = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseText);
        var root = jsonDoc.RootElement;

        root.GetProperty("errorCode").GetString().Should().Be(QuickSignDomainErrorCodes.MaxActiveCertsReached);
        root.GetProperty("httpStatus").GetInt32().Should().Be(400);
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
        root.TryGetProperty("traceId", out _).Should().BeTrue();

        root.GetProperty("details").GetProperty("field").GetString().Should().Be("userId");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_ShouldReturnInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        RequestDelegate next = _ => throw new NullReferenceException("Unexpected null pointer");
        var middleware = new BusinessExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/json");

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBodyStream);
        var responseText = await reader.ReadToEndAsync();

        using var jsonDoc = JsonDocument.Parse(responseText);
        var root = jsonDoc.RootElement;

        root.GetProperty("errorCode").GetString().Should().Be(QuickSignDomainErrorCodes.InternalServerError);
        root.GetProperty("httpStatus").GetInt32().Should().Be(500);
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
        root.TryGetProperty("traceId", out _).Should().BeTrue();
    }
}
