using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Shared;

namespace QuickSign.Infrastructure.Identity;

/// <summary>
/// Service extracting the authenticated user identity and claims from the current HttpContext.
/// Supports normalized claim parsing for SSO JWT tokens.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId
    {
        get
        {
            var user = User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return null;

            return user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    public string GetRequiredUserId()
    {
        return UserId ?? throw new BusinessException(
            QuickSignDomainErrorCodes.Unauthorized,
            "User is not authenticated.",
            401);
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || User == null)
            return false;

        var targetRole = role.Trim().ToLowerInvariant();

        // Check both standard 'role' and Microsoft URI claim
        return User.Claims
            .Where(c => c.Type == "role" || c.Type == ClaimTypes.Role || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(r => string.Equals(r.Trim(), targetRole, StringComparison.OrdinalIgnoreCase));
    }
}
