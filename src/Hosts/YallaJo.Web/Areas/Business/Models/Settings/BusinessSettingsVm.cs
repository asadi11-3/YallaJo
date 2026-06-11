using YallaJo.Web.Areas.Business.Models.Accessibility;
using YallaJo.Web.Areas.Business.Models.Hours;

namespace YallaJo.Web.Areas.Business.Models.Settings;

/// <summary>
/// Combined view model for the consolidated Settings page that hosts the
/// opening-hours and accessibility tabs (view reduction: Hours/Index +
/// Accessibility/Index merged into MyBusinesses/Settings).
/// </summary>
public sealed class BusinessSettingsVm
{
    public const string TabHours = "hours";
    public const string TabAccessibility = "accessibility";

    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";

    /// <summary>"hours" or "accessibility" — which tab is active (server-driven).</summary>
    public string ActiveTab { get; set; } = TabHours;

    public HoursVm Hours { get; set; } = new();
    public AccessibilityVm Accessibility { get; set; } = new();
}
