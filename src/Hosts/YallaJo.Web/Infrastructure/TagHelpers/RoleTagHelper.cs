using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using YallaJo.Web.Infrastructure.Identity;

namespace YallaJo.Web.Infrastructure.TagHelpers;

/// <summary>
/// Razor TagHelper for role-level visibility in views.
///
/// Usage:
///   &lt;role require="Admin"&gt;
///       ... button / content ...
///   &lt;/role&gt;
///
/// Renders inner content ONLY when the current user belongs to the specified role.
/// When the role is absent the entire block (including the wrapper tag) is suppressed.
///
/// Mirrors <see cref="PermissionTagHelper"/> in structure and DB-fallback behaviour:
///   1. Prefers the DB-backed GET /security/me role snapshot (ViewData["SecurityMeRoles"])
///      set by the AdminNav view component.
///   2. Falls back to the request's JWT-claim roles via <see cref="ICurrentUser.IsInRole"/>
///      when the snapshot is unavailable (ERR3).
///
/// This is the ONLY permitted way to hide UI based on roles in Razor views.
/// ViewModels must NOT carry IsAdmin / IsOwner / etc. boolean flags.
/// Views must NOT call ICurrentUser directly or parse claims directly.
/// </summary>
[HtmlTargetElement("role", Attributes = RequireAttributeName)]
public sealed class RoleTagHelper : TagHelper
{
    private const string RequireAttributeName = "require";

    /// <summary>
    /// ViewData key set by the AdminNav view component carrying the DB-backed GET /security/me
    /// role snapshot. When present it is the authoritative source; otherwise the helper falls
    /// back to the request's JWT-claim roles (ERR3).
    /// </summary>
    private const string SecurityMeRolesKey = "SecurityMeRoles";

    private readonly ICurrentUser _currentUser;

    /// <summary>The role name to check (e.g. "Admin", "SuperAdmin", "Owner").</summary>
    [HtmlAttributeName(RequireAttributeName)]
    public string Require { get; set; } = string.Empty;

    /// <summary>Ambient view context, used to read the DB-backed role snapshot when available.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public RoleTagHelper(ICurrentUser currentUser) => _currentUser = currentUser;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Always render as a transparent fragment — never emit the <role> tag itself.
        output.TagName = null;

        if (!IsInRole(Require))
        {
            // User does not have the role — suppress the entire inner content.
            output.SuppressOutput();
            return;
        }

        // User has the role — render the inner content as-is.
        var inner = await output.GetChildContentAsync();
        output.Content.SetHtmlContent(inner);
    }

    /// <summary>
    /// Prefers the DB-backed GET /security/me role snapshot (when the AdminNav view component
    /// populated it) so navigation/UI gating is driven by the database, not raw JWT claims.
    /// Falls back to <see cref="ICurrentUser.IsInRole"/> otherwise (ERR3).
    /// </summary>
    private bool IsInRole(string role)
    {
        if (ViewContext?.ViewData[SecurityMeRolesKey] is IReadOnlyCollection<string> dbRoles)
        {
            return dbRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        return _currentUser.IsInRole(role);
    }
}
