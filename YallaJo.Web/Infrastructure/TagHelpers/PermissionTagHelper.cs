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

    private readonly ICurrentUser _currentUser;

    /// <summary>The permission constant to check (e.g. WebPermission.Role.Create).</summary>
    [HtmlAttributeName(RequireAttributeName)]
    public string Require { get; set; } = string.Empty;

    public PermissionTagHelper(ICurrentUser currentUser) => _currentUser = currentUser;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Always render as a transparent fragment — never emit the <permission> tag itself.
        output.TagName = null;

        if (!_currentUser.HasPermission(Require))
        {
            // User lacks the permission — suppress the entire inner content.
            output.SuppressOutput();
            return;
        }

        // User has the permission — render the inner content as-is.
        var inner = await output.GetChildContentAsync();
        output.Content.SetHtmlContent(inner);
    }
}
