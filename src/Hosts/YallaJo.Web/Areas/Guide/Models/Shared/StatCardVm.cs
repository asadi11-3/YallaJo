namespace YallaJo.Web.Areas.Guide.Models.Shared;

/// <summary>
/// Model for the shared <c>_GuideStatCard</c> partial. <paramref name="Label"/> must already
/// be localized; <paramref name="Value"/> must already be formatted (use <see cref="Infrastructure.GuideFormat"/>).
/// </summary>
public sealed record StatCardVm(
    string Label,
    string Value,
    string Icon,
    string Color);
