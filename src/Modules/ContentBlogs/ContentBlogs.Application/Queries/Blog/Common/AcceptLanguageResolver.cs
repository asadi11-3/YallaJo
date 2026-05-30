using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentBlogs.Application.Queries.Blog.Common;

internal static class AcceptLanguageResolver
{
    public static async Task<ActiveLanguage?> ResolveAsync(
        string? acceptLanguage,
        IActiveLanguageProvider activeLanguageProvider,
        CancellationToken ct)
    {
        var requested = ParseLeadingLanguageCode(acceptLanguage);
        if (string.IsNullOrEmpty(requested))
            return null;

        var languages = await activeLanguageProvider.GetActiveLanguagesAsync(ct).ConfigureAwait(false);
        if (languages.Count == 0)
            return null;
  
        var match = languages.FirstOrDefault(l =>
            string.Equals(l.Code, requested, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return match;

        var dash = requested.IndexOf('-');
        if (dash > 0)
        {
            var neutral = requested[..dash];
            match = languages.FirstOrDefault(l =>
                string.Equals(l.Code, neutral, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return null;
    }

    private static string? ParseLeadingLanguageCode(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        var first = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
            return null;

        return first
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
            .Trim()
            .ToLowerInvariant();
    }
}
