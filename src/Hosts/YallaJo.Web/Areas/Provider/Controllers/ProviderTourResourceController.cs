using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// Shared base for the tour-editor controller family (Tours, TourPricing,
/// TourSchedules, TourWaypoints, TourAvailability, TourGuides, TourImages,
/// TourApplications).
/// <para>
/// Centralizes the four helpers that were previously copy-pasted verbatim
/// (~210 duplicated lines across the family): facade-validation projection,
/// the "denied → tours list" redirect, the "not found" redirect, and the
/// "no provider permission → application status page" redirect.
/// </para>
/// <para>
/// Note on authorization style: these controllers intentionally check
/// permissions imperatively via <c>ICurrentUser.HasPermission</c> instead of
/// <c>[RequirePermission]</c> — a failed imperative check redirects the user
/// to <c>/provider/status</c> (apply-flow UX), whereas the attribute would
/// render the 403 AccessDenied page. Keep it imperative.
/// </para>
/// </summary>
public abstract class ProviderTourResourceController : BaseController
{
    /// <summary>
    /// Copies facade validation errors into ModelState. When no field errors
    /// exist, adds <paramref name="fallback"/> as a model-level error; when
    /// field errors were applied and a fallback is present, flashes it.
    /// <paramref name="keyPrefix"/> supports nested form VMs
    /// (e.g. <c>"Assign."</c> on the guides page).
    /// </summary>
    protected void ApplyFacadeValidation(
        IReadOnlyDictionary<string, string[]>? errors,
        string? fallback,
        string? keyPrefix = null)
    {
        var applied = false;
        if (errors is { Count: > 0 })
        {
            foreach (var (field, messages) in errors)
            {
                var key = string.IsNullOrWhiteSpace(field) ? string.Empty : field;
                foreach (var message in messages)
                    ModelState.AddModelError(keyPrefix is null ? key : keyPrefix + key, message);
                applied = true;
            }
        }

        if (!applied)
            ModelState.AddModelError(string.Empty, fallback ?? L["Provider.Flash.FixHighlighted"].Value);
        else if (!string.IsNullOrWhiteSpace(fallback))
            SetError(fallback);
    }

    /// <summary>Flash an access-denied message and bounce to the provider's tours list.</summary>
    protected IActionResult Denied(string? message)
    {
        SetError(message ?? L["Provider.Flash.NoAccess"].Value);
        return RedirectToTours();
    }

    /// <summary>
    /// Flash a not-found message; redirect to the current controller's own
    /// Index for the tour when <paramref name="tourId"/> is known, otherwise
    /// to the tours list. <paramref name="notFoundFallback"/> lets callers
    /// keep their original wording (e.g. L["Provider.Flash.ListingNotFound"].Value).
    /// </summary>
    protected IActionResult NotFoundRedirect(string? message, Guid? tourId = null, string? notFoundFallback = null)
    {
        SetError(message ?? notFoundFallback ?? L["Provider.Flash.NotFound"].Value);
        return tourId.HasValue
            ? RedirectToAction("Index", new { id = tourId.Value })
            : RedirectToTours();
    }

    /// <summary>Redirect to the provider application status page (no provider permission yet).</summary>
    protected IActionResult RedirectToStatus() =>
        RedirectToAction("Status", "Provider", new { area = "Provider" });

    /// <summary>Redirect to the provider tours list.</summary>
    protected IActionResult RedirectToTours() =>
        RedirectToAction("Index", "Tours", new { area = "Provider" });
}
