using QuickSign.Application.Contracts.DTOs.Auth;

namespace QuickSign.Application.Contracts.Infrastructure;

/// <summary>
/// Infrastructure contract for communicating with the external SSO Identity Provider.
/// </summary>
public interface ISsoIdentityService
{
    Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
