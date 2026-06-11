namespace YallaJo.Web.Areas.Admin.Models.Shared;

/// <summary>
/// View model for the shared <c>Partials/_StatusBadge</c> partial — the canonical
/// admin badge per contract §4.2: <c>badge bg-{color} bg-opacity-10 text-{color}</c>
/// with icon + localized text so status is never conveyed by colour alone (A11Y5).
/// </summary>
public sealed class StatusBadgeVm
{
    /// <summary>Bootstrap colour token: success, warning, danger, info, secondary or dark.</summary>
    public required string Color { get; init; }

    /// <summary>Full Font Awesome icon class, e.g. <c>fa-solid fa-circle-check</c>.</summary>
    public required string Icon { get; init; }

    /// <summary>Pre-localized badge text.</summary>
    public required string Text { get; init; }

    /// <summary>Optional accessible label; rendered as <c>aria-label</c> when present.</summary>
    public string? AriaLabel { get; init; }
}
