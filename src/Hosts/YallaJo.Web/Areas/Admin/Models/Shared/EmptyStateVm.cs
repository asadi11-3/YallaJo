namespace YallaJo.Web.Areas.Admin.Models.Shared;

/// <summary>
/// View model for the shared <c>Partials/_EmptyState</c> partial (contract §4.9, L6).
/// The caller passes pre-localized strings; the optional CTA renders only when both
/// <see cref="CtaText"/> and <see cref="CtaUrl"/> are supplied (compute the URL via
/// <c>Url.Action(...)</c> at the call site).
/// </summary>
public sealed class EmptyStateVm
{
    public required string Message { get; init; }

    /// <summary>Optional pre-localized heading rendered as an <c>h2.h5</c> above the message.</summary>
    public string? Heading { get; init; }

    public string? CtaText { get; init; }

    public string? CtaUrl { get; init; }

    /// <summary>App-relative image path; resolved with <c>Url.Content</c> in the partial.</summary>
    public string ImageSrc { get; init; } = "~/assets/images/element/17.svg";

    /// <summary>Container CSS classes — override to keep pixel parity with pre-existing variants
    /// (e.g. <c>card-body text-center py-5</c>, <c>text-center py-4</c>).</summary>
    public string ContainerClass { get; init; } = "text-center py-5";

    /// <summary>Image CSS classes — override for variants such as <c>h-80px mb-3 mx-auto</c>.</summary>
    public string ImageClass { get; init; } = "h-80px mb-3";

    public bool HasCta => !string.IsNullOrEmpty(CtaText) && !string.IsNullOrEmpty(CtaUrl);
}
