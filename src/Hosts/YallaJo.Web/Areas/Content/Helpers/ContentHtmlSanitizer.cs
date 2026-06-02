using Ganss.Xss;

namespace YallaJo.Web.Areas.Content.Helpers;

/// <summary>
/// Reusable HTML sanitizer for Content-area features that render user/creator
/// authored rich text with <c>Html.Raw</c> (e.g. blog bodies).
/// <para>
/// YallaJo.Api stores such content verbatim (its only HTML handling is
/// validation-time tag stripping for word-count, never persistence sanitization),
/// so the BFF is the trust boundary. This allowlist keeps safe rich-text formatting
/// while removing script/iframe/object/embed/form/event-handler and
/// <c>javascript:</c>-style vectors.
/// </para>
/// </summary>
public static class ContentHtmlSanitizer
{
    // HtmlSanitizer is safe to reuse across threads once configured; sanitizing is
    // a pure read of the configured allowlists. Build one configured instance.
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    private static readonly string[] AllowedTags =
    {
        // Block + inline text formatting
        "p", "br", "span", "div",
        "strong", "b", "em", "i", "u", "s", "small", "mark", "sub", "sup",
        // Headings (template uses h2–h4 for article structure)
        "h2", "h3", "h4", "h5", "h6",
        // Lists
        "ul", "ol", "li",
        // Quotes / code
        "blockquote", "pre", "code",
        // Links + media
        "a", "img",
        // Tables
        "table", "thead", "tbody", "tfoot", "tr", "th", "td",
        // Misc structural
        "hr", "figure", "figcaption",
    };

    private static readonly string[] AllowedAttributes =
    {
        "href", "src", "alt", "title", "target", "rel",
        "width", "height", "colspan", "rowspan", "class",
    };

    private static readonly string[] AllowedSchemes = { "http", "https", "mailto" };

    /// <summary>
    /// Returns a sanitized copy of <paramref name="html"/> safe for
    /// <c>Html.Raw</c>. Null/whitespace input yields <see cref="string.Empty"/>.
    /// </summary>
    public static string Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? string.Empty : Sanitizer.Sanitize(html);

    private static HtmlSanitizer CreateSanitizer()
    {
        // Default ctor seeds a sane baseline; we then narrow every allowlist to a
        // strict, explicit set so nothing is permitted unless listed here.
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in AllowedTags)
            sanitizer.AllowedTags.Add(tag);

        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in AllowedAttributes)
            sanitizer.AllowedAttributes.Add(attr);

        // Strip all inline CSS and CSS classes-by-allowlist — no styles needed for
        // article body text and they are a common bypass surface.
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedClasses.Clear();
        sanitizer.AllowedAtRules.Clear();

        // Only safe link/media protocols. Dropping the rest removes javascript:,
        // data:, vbscript:, etc.
        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in AllowedSchemes)
            sanitizer.AllowedSchemes.Add(scheme);

        // Never keep data-* attributes or event handlers (onclick is already excluded
        // by the attribute allowlist; this is defense in depth).
        sanitizer.AllowDataAttributes = false;

        return sanitizer;
    }
}
