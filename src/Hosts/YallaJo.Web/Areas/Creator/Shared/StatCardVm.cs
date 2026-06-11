namespace YallaJo.Web.Areas.Creator.Shared;

/// <summary>
/// Model for the shared <c>_StatCard</c> partial used by the creator dashboard and preview pages.
/// When <see cref="Icon"/> is provided the card renders the icon variant (dashboard);
/// otherwise it renders the compact centered variant (preview).
/// </summary>
/// <param name="Value">Pre-formatted, culture-aware count text (CON3).</param>
/// <param name="Label">Localized label text.</param>
/// <param name="Icon">Optional Font Awesome icon class (e.g. <c>fa-book-open</c>).</param>
/// <param name="ColorClass">Optional Bootstrap color suffix (e.g. <c>success</c>); required when <see cref="Icon"/> is set.</param>
public sealed record StatCardVm(string Value, string Label, string? Icon = null, string? ColorClass = null);
