namespace QuickSign.Application.Contracts.DTOs.Auth;

public sealed class LoginRequestDto
{
    public string UsernameOrEmail { get; set; } = default!;
    public string Password { get; set; } = default!;
}
