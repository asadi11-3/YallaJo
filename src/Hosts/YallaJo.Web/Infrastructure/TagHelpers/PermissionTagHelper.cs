using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using YallaJo.Web.Infrastructure.Identity;

namespace YallaJo.Web.Infrastructure.TagHelpers;

/// <summary>
/// Razor TagHelper for action-level visibility in views.
///
/// Usage:
///   &lt;permission require="@WebPermission.Role.Create"&gt;
///       ... create form HTML ...
///   &lt;/permission&gt;
///
/// Renders inner content ONLY when the current user has the specified permission.
/// When the permission is absent the entire block (including the wrapper tag) is suppressed.
///
/// This is the ONLY permitted way to hide UI based on permissions in Razor views.
/// ViewModels must NOT carry CanCreate / CanUpdate / etc. boolean flags.
/// Views must NOT call ICurrentUser directly or parse claims directly.
///
/// The TagHelper receives ICurrentUser via DI (constructor injection works because
/// MVC creates TagHelper instances through the DI container when registered via
/// @addTagHelper *, YallaJo.Web in _ViewImports.cshtml).
/// </summary>
[HtmlTargetElement("permission", Attributes = RequireAttributeName)]
public sealed class PermissionTagHelper : TagHelper
{
    private const string RequireAttributeName = "require";

    /// <summary>
    /// ViewData key set by the AdminNav view component carrying the DB-backed GET /security/me
    /// permission snapshot. When present it is the authoritative source (plan §9 line 13);
    /// otherwise the helper falls back to the request's JWT-claim permissions (ERR3).
    /// </summary>
    private const string SecurityMePermissionsKey = "SecurityMePermissions";

    private readonly ICurrentUser _currentUser;

    /// <summary>The permission constant to check (e.g. WebPermission.Role.Create).</summary>
    [HtmlAttributeName(RequireAttributeName)]
    public string Require { get; set; } = string.Empty;

    /// <summary>Ambient view context, used to read the DB-backed permission snapshot when available.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public PermissionTagHelper(ICurrentUser currentUser) => _currentUser = currentUser;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Always render as a transparent fragment — never emit the <permission> tag itself.
        output.TagName = null;

        if (!HasPermission(Require))
        {
            // User lacks the permission — suppress the entire inner content.
            output.SuppressOutput();
            return;
        }

        // User has the permission — render the inner content as-is.
        var inner = await output.GetChildContentAsync();
        output.Content.SetHtmlContent(inner);
    }

    /// <summary>
    /// Prefers the DB-backed GET /security/me permission snapshot (when the AdminNav view
    /// component populated it) so navigation/UI gating is driven by the database, not raw
    /// JWT claims (plan §9 line 13). Falls back to <see cref="ICurrentUser"/> otherwise (ERR3).
    /// </summary>
    private bool HasPermission(string permission)
    {
        if (ViewContext?.ViewData[SecurityMePermissionsKey] is IReadOnlyCollection<string> dbPermissions)
        {
            return dbPermissions.Contains(permission);
        }

        return _currentUser.HasPermission(permission);
    }
}
