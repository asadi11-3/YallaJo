using System.Security.Claims;

namespace YallaJo.SharedKernel.Application.Abstractions.Context;

/// <summary>
/// Represents the currently authenticated user extracted from the ClaimsPrincipal.
/// Registered as scoped in the API host.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    IEnumerable<Claim> Claims { get; }
    string? GetClaim(string claimType);
    bool HasPermission(string permission);
    bool IsInRole(string role);
}
