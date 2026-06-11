namespace YallaJo.Web.Areas.Guide.Models.Shared;

/// <summary>
/// Model for the shared <c>_GuideEmptyState</c> partial (UI-L6: empty states with optional CTA).
/// <paramref name="Message"/> and <paramref name="CtaText"/> must already be localized.
/// </summary>
public sealed record EmptyStateVm(
    string Message,
    string? CtaText = null,
    string? CtaHref = null);
