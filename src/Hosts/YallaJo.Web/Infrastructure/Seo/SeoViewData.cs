using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace YallaJo.Web.Infrastructure.Seo;

/// <summary>
/// Bridges a <see cref="SeoModel"/> into the ViewData keys that the shared layout head reads.
/// Keep these key names in sync with ~/Views/Shared/_Layout.cshtml.
/// </summary>
public static class SeoViewData
{
    public const string TitleKey = "Title";
    public const string DescriptionKey = "MetaDescription";
    public const string CanonicalKey = "Canonical";
    public const string OgImageKey = "OgImage";
    public const string OgTypeKey = "OgType";
    public const string RobotsKey = "Robots";
    public const string JsonLdKey = "JsonLd";

    private const int TitleMaxLength = 60;
    private const int DescriptionMaxLength = 160;

    /// <summary>Writes the SEO model into <paramref name="viewData"/> for the layout head to render.</summary>
    public static void SetSeo(this ViewDataDictionary viewData, SeoModel seo)
    {
        ArgumentNullException.ThrowIfNull(viewData);
        ArgumentNullException.ThrowIfNull(seo);

        viewData[TitleKey] = Truncate(seo.Title, TitleMaxLength);
        viewData[DescriptionKey] = Truncate(seo.Description, DescriptionMaxLength);
        viewData[CanonicalKey] = NullIfBlank(seo.Canonical);
        viewData[OgImageKey] = NullIfBlank(seo.OgImage);
        viewData[OgTypeKey] = NullIfBlank(seo.OgType) ?? "website";
        viewData[RobotsKey] = NullIfBlank(seo.Robots);
        viewData[JsonLdKey] = NullIfBlank(seo.JsonLd);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max].TrimEnd();
    }
}
