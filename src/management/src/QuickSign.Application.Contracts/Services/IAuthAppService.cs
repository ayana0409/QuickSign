using QuickSign.Application.Contracts.DTOs.Auth;

namespace QuickSign.Application.Contracts.Services;

public interface IAuthAppService
{
    Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
