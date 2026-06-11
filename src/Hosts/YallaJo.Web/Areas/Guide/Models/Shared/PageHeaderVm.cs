namespace YallaJo.Web.Areas.Guide.Models.Shared;

/// <summary>
/// View model for the shared _GuidePageHeader partial (A11Y6: exactly one h1 per page).
/// <paramref name="Title"/> and <paramref name="Subtitle"/> must be pre-localized.
/// Pages needing an action button place the partial inside their own flex wrapper.
/// </summary>
public sealed record PageHeaderVm(string Title, string Icon, string? Subtitle = null);
