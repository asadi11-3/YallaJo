using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using YallaJo.SharedKernel.Application.Abstractions.Context;

namespace YallaJo.Api.Services;

/// <summary>
/// Reads identity data from the current ClaimsPrincipal (populated by JWT middleware).
///
/// <para>
/// JWT-401 hardening: claim reads are intentionally <b>defensive</b> across the
/// known claim-type spellings so the API keeps working regardless of whether
/// <c>JwtBearerOptions.MapInboundClaims</c> is honoured by the runtime:
/// </para>
///
/// <list type="bullet">
///   <item><b>UserId</b>: <c>"sub"</c> → <see cref="ClaimTypes.NameIdentifier"/> → <c>"nameid"</c></item>
///   <item><b>Roles</b>: <c>"role"</c> ∪ <see cref="ClaimTypes.Role"/></item>
///   <item><b>Permissions</b>: <c>"Permission"</c> ∪ <c>"permission"</c></item>
/// </list>
///
/// <para>
/// The token issuer (<c>JwtTokenService</c>) emits <c>JwtRegisteredClaimNames.Sub</c>
/// (raw <c>"sub"</c>), <c>"role"</c>, and (via <c>AdditionalClaims</c>) capital-P
/// <c>"Permission"</c>.  When <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions.MapInboundClaims"/>
/// is silently ignored by the runtime, <c>JwtSecurityTokenHandler</c> auto-maps
/// <c>"sub"</c> → <see cref="ClaimTypes.NameIdentifier"/> and <c>"role"</c> →
/// <see cref="ClaimTypes.Role"/>.  Reading both forms makes the API resilient to
/// either runtime behaviour and to mixed token sources.
/// </para>
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            // JWT-401 hardening: try every known spelling for the user-id claim.
            // - "sub"              — what JwtTokenService emits and what
            //                        MapInboundClaims=false should preserve.
            // - ClaimTypes.NameIdentifier
            //                      — what JwtSecurityTokenHandler maps "sub" to
            //                        when MapInboundClaims is silently honoured
            //                        (or a stale runtime ignores the option).
            // - "nameid"           — short alias some libraries emit.
            var raw = User?.FindFirstValue("sub")
                   ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User?.FindFirstValue("nameid");

            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? UserName =>
        User?.FindFirstValue("name")
        ?? User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.FindFirstValue("sub");

    public string? Email =>
        User?.FindFirstValue("email")
        ?? User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles
    {
        get
        {
            // JWT-401 hardening: the token issuer emits "role" but the runtime
            // may auto-map it to ClaimTypes.Role when MapInboundClaims is
            // silently honoured.  Read both, distinct.
            if (User is null) return [];
            return User.FindAll("role")
                .Concat(User.FindAll(ClaimTypes.Role))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }

    public IEnumerable<string> Permissions
    {
        get
        {
            // JWT-401 hardening: the token issuer emits capital-P "Permission"
            // (per AppClaimTypes.Permission and the project's permission catalog
            // convention).  Earlier code on the API side was reading lowercase
            // "permission" only, which silently dropped every permission and
            // caused MustHavePermission gates to deny.  Read BOTH spellings
            // case-sensitively so a future migration to either spelling stays
            // covered.
            if (User is null) return [];
            return User.FindAll("Permission")
                .Concat(User.FindAll("permission"))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }

    public IEnumerable<Claim> Claims => User?.Claims ?? [];

    public string? GetClaim(string claimType) =>
        User?.FindFirstValue(claimType);

    // "*" wildcard permission (super-roles Owner / SuperAdmin) grants every
    // permission. These roles no longer carry the full explicit permission array
    // in their JWT (it was dropped to keep the Bearer header under the IIS
    // request-header size limit on shared hosting), so the wildcard MUST be
    // honored here or imperative HasPermission checks would deny super-admins.
    private const string WildcardPermission = "*";

    public bool HasPermission(string permission) =>
        Permissions.Contains(WildcardPermission, StringComparer.OrdinalIgnoreCase)
        || Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
