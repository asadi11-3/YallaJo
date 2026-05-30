using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ContentBlogs.Application.Commands.Blog.Common;

/// <summary>
/// Deterministic slug generator for the Blog aggregate.
/// Used when a caller omits an explicit slug — the title is normalized to a
/// lowercase, hyphen-separated, ASCII-only token compatible with
/// <see cref="ContentBlogs.Domain.Entities.Blog"/> slug rules.
/// </summary>
internal static class BlogSlugGenerator
{
    /// <summary>
    /// Maximum slug length accepted by the Create/Update validator (kept in sync
    /// with <c>RuleFor(x =&gt; x.Slug).MaximumLength(200)</c>).  Generated slugs
    /// are truncated to this length to guarantee the validator/domain accept them.
    /// </summary>
    public const int MaxSlugLength = 200;

    private static readonly Regex InvalidCharsRegex =
        new("[^a-z0-9-]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly Regex MultipleDashesRegex =
        new("-{2,}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Returns a normalized slug.  If <paramref name="explicitSlug"/> is provided
    /// it is trimmed and lower-cased only — explicit slugs are <b>not</b> stripped
    /// of invalid characters so the validator can reject them strictly.
    /// Otherwise <paramref name="title"/> is slugified into an ASCII-safe token.
    /// </summary>
    public static string Generate(string? explicitSlug, string title)
    {
        if (!string.IsNullOrWhiteSpace(explicitSlug))
            return explicitSlug.Trim().ToLowerInvariant();

        return FromTitle(title);
    }

    private static string FromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // Decompose accented characters and strip non-ASCII marks (e.g. "café" → "cafe").
        var normalized = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        var ascii = builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Trim()
            .Replace(' ', '-')
            .Replace('_', '-');

        ascii = InvalidCharsRegex.Replace(ascii, string.Empty);
        ascii = MultipleDashesRegex.Replace(ascii, "-").Trim('-');

        if (ascii.Length > MaxSlugLength)
        {
            ascii = ascii[..MaxSlugLength].TrimEnd('-');
            // Truncation could have left a trailing hyphen mid-segment — strip it
            // again to guarantee the slug regex still matches.
            ascii = MultipleDashesRegex.Replace(ascii, "-").Trim('-');
        }

        return ascii;
    }
}
