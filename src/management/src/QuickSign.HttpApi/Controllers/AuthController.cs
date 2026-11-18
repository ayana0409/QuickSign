using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickSign.Application.Contracts.DTOs.Auth;
using QuickSign.Application.Contracts.Services;

namespace QuickSign.HttpApi.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController : QuickSignControllerBase
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<AuthTokenResponseDto> LoginAsync([FromBody] LoginRequestDto input, CancellationToken cancellationToken)
    {
        return _authAppService.LoginAsync(input, cancellationToken);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public Task RegisterAsync([FromBody] RegisterRequestDto input, CancellationToken cancellationToken)
    {
        return _authAppService.RegisterAsync(input, cancellationToken);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<AuthTokenResponseDto> RefreshTokenAsync([FromBody] string refreshToken, CancellationToken cancellationToken)
    {
        return _authAppService.RefreshTokenAsync(refreshToken, cancellationToken);
    }
}
