using QuickSign.Application.Contracts.DTOs.Auth;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Contracts.Services;

namespace QuickSign.Application.Services;

/// <summary>
/// Application service implementing user authentication workflows via external SSO Identity integration.
/// </summary>
public sealed class AuthAppService : IAuthAppService
{
    private readonly ISsoIdentityService _ssoIdentityService;

    public AuthAppService(ISsoIdentityService ssoIdentityService)
    {
        _ssoIdentityService = ssoIdentityService;
    }

    public Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        return _ssoIdentityService.LoginAsync(request, cancellationToken);
    }

    public Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        return _ssoIdentityService.RegisterAsync(request, cancellationToken);
    }

    public Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return _ssoIdentityService.RefreshTokenAsync(refreshToken, cancellationToken);
    }
}
