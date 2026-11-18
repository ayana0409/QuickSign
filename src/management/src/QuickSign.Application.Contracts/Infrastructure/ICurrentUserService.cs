namespace QuickSign.Application.Contracts.Infrastructure;

public interface ICurrentUserService
{
    string? UserId { get; }
    string GetRequiredUserId();
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
