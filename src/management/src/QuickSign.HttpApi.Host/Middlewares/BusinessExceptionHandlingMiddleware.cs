using System.Diagnostics;
using System.Text.Json;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Shared;

namespace QuickSign.HttpApi.Host.Middlewares;

/// <summary>
/// Global exception handling middleware converting domain and system exceptions into
/// unified SCREAMING_SNAKE_CASE JSON error responses per AGENT.md specification.
/// </summary>
public sealed class BusinessExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BusinessExceptionHandlingMiddleware> _logger;

    public BusinessExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<BusinessExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Handled business exception: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            await HandleBusinessExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled server exception: {Message}", ex.Message);
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static async Task HandleBusinessExceptionAsync(HttpContext context, BusinessException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = ex.HttpStatus;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var payload = new Dictionary<string, object?>
        {
            ["errorCode"] = ex.ErrorCode,
            ["httpStatus"] = ex.HttpStatus,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["traceId"] = traceId
        };

        if (ex.Details != null && ex.Details.Count > 0)
        {
            payload["details"] = ex.Details;
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }

    private static async Task HandleUnhandledExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var payload = new Dictionary<string, object?>
        {
            ["errorCode"] = QuickSignDomainErrorCodes.InternalServerError,
            ["httpStatus"] = StatusCodes.Status500InternalServerError,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["traceId"] = traceId
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}
