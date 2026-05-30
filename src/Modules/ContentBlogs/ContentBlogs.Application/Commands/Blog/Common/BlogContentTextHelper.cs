using System.Text.RegularExpressions;

namespace ContentBlogs.Application.Commands.Blog.Common;

/// <summary>
/// Shared text utilities for the Blog aggregate.  Centralises the HTML-stripping
/// rule used by both validators and read-time estimation so every layer sees the
/// same "meaningful text" view of the content.
/// </summary>
internal static class BlogContentTextHelper
{
    private static readonly Regex StripHtmlRegex =
        new("<.*?>", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly char[] WordSplitters =
        new[] { ' ', '\t', '\n', '\r' };

    /// <summary>
    /// Returns the supplied content with HTML tags removed and surrounding
    /// whitespace trimmed.  Empty input → <see cref="string.Empty"/>.
    /// </summary>
    public static string StripHtml(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        return StripHtmlRegex.Replace(content, string.Empty).Trim();
    }

    /// <summary>
    /// Counts whitespace-separated tokens in HTML-stripped content.
    /// </summary>
    public static int CountWords(string? content)
    {
        var stripped = StripHtml(content);
        if (stripped.Length == 0)
            return 0;

        return stripped.Split(WordSplitters, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
