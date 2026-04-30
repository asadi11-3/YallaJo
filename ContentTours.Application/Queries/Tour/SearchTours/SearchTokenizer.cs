using System.Text.RegularExpressions;

namespace ContentTours.Application.Queries.Tour.SearchTours;

/// <summary>
/// Pure static helper for search query tokenization.
/// Rules: trim, lowercase, split on whitespace, drop tokens &lt;2 chars, dedup, cap at 10.
/// </summary>
public static class SearchTokenizer
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<string> Tokenize(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return [];

        var tokens = WhitespaceRegex
            .Split(q.Trim().ToLowerInvariant())
            .Where(t => t.Length >= 2)
            .Distinct()
            .Take(10)
            .ToList();

        return tokens;
    }
}
