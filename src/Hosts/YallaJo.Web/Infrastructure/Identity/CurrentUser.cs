using System.Security.Claims;
using YallaJo.Web.Infrastructure.Authentication.Claims;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Infrastructure.Identity;

/// <summary>
/// Scoped implementation of <see cref="ICurrentUser"/>.
/// <para>
/// Role and Permission claims are NOT stored in the authentication cookie (that
/// duplication previously bloated the admin cookie past Kestrel's 32 KB header
/// limit → HTTP 431). Instead they are derived on demand from the
/// <c>access_token</c> JWT embedded in the cookie and cached for the lifetime of
/// the request, so repeated HasPermission / IsInRole calls remain O(1) and the JWT
/// is parsed at most once per request.
/// </para>
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

    // A principal holding the "*" wildcard permission (super-roles Owner /
    // SuperAdmin) is granted EVERY permission. These roles no longer carry the
    // full explicit permission array in their JWT (it was dropped to keep the
    // token small enough for the IIS request-header limit), so the wildcard MUST
    // be honored here or admin nav / RequirePermission-gated pages would break.
    private const string WildcardPermission = "*";

    public bool HasPermission(string permission) =>
        Permissions.Contains(WildcardPermission) || Permissions.Contains(permission);

    public bool IsInRole(string role) =>
        Roles.Contains(role);

    private ClaimsPrincipal? Principal =>
        _principal ??= _accessor.HttpContext?.User;

    // Role + Permission claims live inside the access_token JWT (not as standalone
    // cookie claims). Parse the JWT once per request and split into the two sets.
    private void EnsureClaimsLoaded()
    {
        if (_permissions is not null && _roles is not null) return;

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var accessToken = Principal?.FindFirstValue(AppClaimTypes.AccessToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            foreach (var claim in WebSignInService.ExtractUserClaimsFromJwt(accessToken))
            {
                if (claim.Type == AppClaimTypes.Permission)
                    permissions.Add(claim.Value);
                else if (claim.Type == AppClaimTypes.Role)
                    roles.Add(claim.Value);
            }
        }

        _permissions = permissions;
        _roles = roles;
    }

    private HashSet<string> Permissions
    {
        get { EnsureClaimsLoaded(); return _permissions!; }
    }

    private HashSet<string> Roles
    {
        get { EnsureClaimsLoaded(); return _roles!; }
    }
}
