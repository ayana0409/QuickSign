using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickSign.Application.Contracts.DTOs.Auth;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Shared;

namespace QuickSign.Infrastructure.Identity;

/// <summary>
/// Infrastructure service integrating with external SSO Identity Server via REST endpoints.
/// Strictly enforces presence of configuration and fails fast if endpoints are missing.
/// </summary>
public sealed class SsoIdentityService : ISsoIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly SsoIdentityOptions _options;
    private readonly ILogger<SsoIdentityService> _logger;

    public SsoIdentityService(
        HttpClient httpClient,
        IOptions<SsoIdentityOptions> options,
        ILogger<SsoIdentityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        // Fail-fast configuration validation
        if (string.IsNullOrWhiteSpace(_options.Authority))
            throw new InvalidOperationException("Configuration 'SsoIdentity:Authority' is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(_options.TokenEndpoint))
            throw new InvalidOperationException("Configuration 'SsoIdentity:TokenEndpoint' is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(_options.RegisterEndpoint))
            throw new InvalidOperationException("Configuration 'SsoIdentity:RegisterEndpoint' is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("Configuration 'SsoIdentity:ClientId' is required and cannot be empty.");
    }

    public async Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["username"] = request.UsernameOrEmail,
            ["password"] = request.Password,
            ["scope"] = EnsureOfflineAccessScope(_options.Scope)
        };

        if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            parameters["client_secret"] = _options.ClientSecret;
        }

        var formContent = new FormUrlEncodedContent(parameters);

        try
        {
            var response = await _httpClient.PostAsync(_options.TokenEndpoint, formContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("SSO login failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);

                throw new BusinessException(
                    QuickSignDomainErrorCodes.InvalidCredentials,
                    "Invalid username or password.",
                    401);
            }

            var tokenDto = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(cancellationToken: cancellationToken);
            if (tokenDto is null)
            {
                throw new BusinessException(
                    QuickSignDomainErrorCodes.InvalidCredentials,
                    "Empty token payload received from Identity Provider.",
                    401);
            }

            if (string.IsNullOrWhiteSpace(tokenDto.RefreshToken))
            {
                _logger.LogWarning(
                    "SSO Identity Provider did not return a refresh_token for client '{ClientId}'. Ensure 'offline_access' scope and refresh_token grant are enabled on the SSO client.",
                    _options.ClientId);
            }

            return tokenDto;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error communicating with SSO Identity TokenEndpoint.");
            throw new BusinessException(
                QuickSignDomainErrorCodes.ServiceUnavailable,
                "Authentication service is temporarily unavailable. Please try again later.",
                503,
                innerException: ex);
        }
    }

    public async Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_options.RegisterEndpoint, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("SSO registration failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);

                throw new BusinessException(
                    QuickSignDomainErrorCodes.RegistrationFailed,
                    $"Account registration failed: {errorBody}",
                    400);
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error communicating with SSO RegisterEndpoint.");
            throw new BusinessException(
                QuickSignDomainErrorCodes.ServiceUnavailable,
                "Registration service is temporarily unavailable. Please try again later.",
                503,
                innerException: ex);
        }
    }

    public async Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.RefreshTokenExpired,
                "Refresh token cannot be empty.",
                401);
        }

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["refresh_token"] = refreshToken,
            ["scope"] = EnsureOfflineAccessScope(_options.Scope)
        };

        if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            parameters["client_secret"] = _options.ClientSecret;
        }

        var formContent = new FormUrlEncodedContent(parameters);

        try
        {
            var response = await _httpClient.PostAsync(_options.TokenEndpoint, formContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("SSO refresh token failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);

                throw new BusinessException(
                    QuickSignDomainErrorCodes.RefreshTokenExpired,
                    "Session refresh token has expired or is invalid.",
                    401);
            }

            var tokenDto = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(cancellationToken: cancellationToken);
            return tokenDto ?? throw new BusinessException(
                QuickSignDomainErrorCodes.RefreshTokenExpired,
                "Empty token payload received on refresh.",
                401);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error communicating with SSO Identity TokenEndpoint during refresh.");
            throw new BusinessException(
                QuickSignDomainErrorCodes.ServiceUnavailable,
                "Authentication service is temporarily unavailable.",
                503,
                innerException: ex);
        }
    }

    /// <summary>
    /// Ensures 'offline_access' is included in the OAuth scope so the SSO Identity Server issues a refresh_token.
    /// </summary>
    private static string EnsureOfflineAccessScope(string? configuredScope)
    {
        var scopes = (configuredScope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (!scopes.Contains("offline_access", StringComparer.OrdinalIgnoreCase))
        {
            scopes.Add("offline_access");
        }

        return string.Join(' ', scopes);
    }
}
