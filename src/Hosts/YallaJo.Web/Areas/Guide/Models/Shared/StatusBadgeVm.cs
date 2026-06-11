namespace YallaJo.Web.Areas.Guide.Models.Shared;

/// <summary>
/// Model for the shared <c>_GuideStatusBadge</c> partial. Maps a raw backend status string
/// (or <c>enum.ToString()</c>) to a Bootstrap color + Font Awesome icon so every badge carries
/// color AND icon AND localized text (A11Y5 — never color-only).
/// The partial localizes via <c>Guide.Status.{Status}</c>.
/// </summary>
public sealed record StatusBadgeVm(string? Status)
{
    /// <summary>Normalized key used for both styling and the resx lookup.</summary>
    public string Key => string.IsNullOrWhiteSpace(Status) ? "Unknown" : Status.Trim();

    public string Color => Key.ToLowerInvariant() switch
    {
        "paid" or "completed" or "approved" or "accepted" or "active" or "published" => "success",
        "rejected" or "declined" or "cancelled" or "refunded" or "removed" => "danger",
        "pending" or "submitted" or "pendingreview" => "warning",
        "expired" or "suspended" or "draft" => "secondary",
        _ => "secondary",
    };

    public string Icon => Key.ToLowerInvariant() switch
    {
        "paid" or "completed" or "approved" or "accepted" or "active" or "published" => "circle-check",
        "rejected" or "declined" or "removed" => "circle-xmark",
        "cancelled" => "ban",
        "refunded" => "rotate-left",
        "pending" or "submitted" or "pendingreview" => "clock",
        "expired" => "hourglass-end",
        "suspended" => "circle-pause",
        "draft" => "pen-ruler",
        _ => "circle-info",
    };
}
