namespace QuickSign.Application.Contracts.DTOs.Auth;

public sealed class RegisterRequestDto
{
    public string UserName { get; set; } = default!;
    public string EmailAddress { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? AppName { get; set; }
}
