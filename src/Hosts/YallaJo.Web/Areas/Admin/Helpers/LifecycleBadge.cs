namespace YallaJo.Web.Areas.Admin.Helpers;

/// <summary>
/// Phase 5D — pure helpers for the user-status badge. Today both
/// <see cref="GetCssClass"/> and <see cref="GetLabel"/> short-circuit
/// to the binary <c>IsActive</c> mapping because the backend
/// <c>UserDto</c> only exposes that boolean.
/// <para>
/// The <c>lifecycleState</c> parameter is accepted (and tested) so a
/// future backend extension projecting <c>"Suspended"</c> /
/// <c>"PendingActivation"</c> / <c>"PendingPasswordReset"</c> /
/// <c>"Provisioned"</c> / <c>"Archived"</c> can be wired up by editing
/// only this helper — every call site is already passing the field.
/// </para>
/// <para>
/// <c>internal</c> so <c>Web.Tests.Unit</c> can call it directly via
/// the <c>InternalsVisibleTo</c> registered in
/// <c>YallaJo.Web.csproj</c>.
/// </para>
/// </summary>
internal static class LifecycleBadge
{
    /// <summary>
    /// Returns the Bootstrap badge CSS class for the supplied lifecycle
    /// inputs. Today: green (<c>bg-success</c>) for active, gray
    /// (<c>bg-secondary</c>) for everything else.
    /// </summary>
    public static string GetCssClass(bool isActive, string? lifecycleState = null)
    {
        // Phase 5D — binary mapping driven by IsActive. The
        // forward-compatible branch is intentionally a no-op until the
        // backend exposes lifecycleState; the parameter is preserved
        // so call sites do not need to change when that lands.
        _ = lifecycleState;
        return isActive ? "bg-success" : "bg-secondary";
    }

    /// <summary>
    /// Returns the human-readable label for the badge. Today:
    /// <c>"Active"</c> when <paramref name="isActive"/> is true,
    /// <c>"Inactive"</c> otherwise.
    /// </summary>
    public static string GetLabel(bool isActive, string? lifecycleState = null)
    {
        _ = lifecycleState;
        return isActive ? "Active" : "Inactive";
    }

    /// <summary>
    /// Returns the Font Awesome icon CSS class for the badge so status is
    /// conveyed by colour + icon + text (A11Y5), not colour alone:
    /// <c>fa-circle-check</c> when active, <c>fa-ban</c> otherwise.
    /// </summary>
    public static string GetIconClass(bool isActive, string? lifecycleState = null)
    {
        _ = lifecycleState;
        return isActive ? "fa-solid fa-circle-check" : "fa-solid fa-ban";
    }
}
