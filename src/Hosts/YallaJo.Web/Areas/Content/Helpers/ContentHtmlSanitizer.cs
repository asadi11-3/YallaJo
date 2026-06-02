using AngleSharp.Css.Dom;
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

    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();
    public static string Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? string.Empty : Sanitizer.Sanitize(html);

    private static HtmlSanitizer CreateSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags          = new HashSet<string>(AllowedTags, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes    = new HashSet<string>(AllowedAttributes, StringComparer.OrdinalIgnoreCase),
            AllowedSchemes       = new HashSet<string>(AllowedSchemes, StringComparer.OrdinalIgnoreCase),
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedCssClasses    = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedAtRules       = new HashSet<CssRuleType>(),
            UriAttributes        = new HashSet<string>(new[] { "href", "src" }, StringComparer.OrdinalIgnoreCase),
        };

        var sanitizer = new HtmlSanitizer(options)
        {

            AllowDataAttributes = false,
        };

        return sanitizer;
    }
}
