using System.Security.Claims;
using YallaJo.Web.Infrastructure.Authentication.Claims;

namespace YallaJo.Web.Infrastructure.Identity;

/// <summary>
/// Scoped implementation of <see cref="ICurrentUser"/>.
/// Reads claims lazily from the current <see cref="ClaimsPrincipal"/> and caches them
/// for the lifetime of the request so repeated HasPermission / IsInRole calls are O(1).
/// </summary>
internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    // ── Lazy per-request cache ────────────────────────────────────────────────
    private ClaimsPrincipal? _principal;
    private HashSet<string>? _permissions;
    private HashSet<string>? _roles;
    private bool _userIdResolved;
    private Guid? _userId;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            if (_userIdResolved) return _userId;
            _userIdResolved = true;
            var raw = Principal?.FindFirstValue(AppClaimTypes.UserId);
            _userId = Guid.TryParse(raw, out var id) ? id : null;
            return _userId;
        }
    }

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission);

    public bool IsInRole(string role) =>
        Roles.Contains(role);

    private ClaimsPrincipal? Principal =>
        _principal ??= _accessor.HttpContext?.User;

    private HashSet<string> Permissions =>
        _permissions ??= Principal?
            .FindAll(AppClaimTypes.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
        ?? [];

    private HashSet<string> Roles =>
        _roles ??= Principal?
            .FindAll(AppClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
        ?? [];
}
