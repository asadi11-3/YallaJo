using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using YallaJo.SharedKernel.Application.Abstractions.Context;

namespace YallaJo.Api.Services;

/// <summary>
/// Reads identity data from the current ClaimsPrincipal (populated by JWT middleware).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? UserName => User?.FindFirstValue("name") ?? User?.FindFirstValue("sub");
    public string? Email => User?.FindFirstValue("email");
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        User?.FindAll("role").Select(c => c.Value) ?? [];

    public IEnumerable<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value) ?? [];

    public IEnumerable<Claim> Claims => User?.Claims ?? [];

    public string? GetClaim(string claimType) =>
        User?.FindFirstValue(claimType);

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
