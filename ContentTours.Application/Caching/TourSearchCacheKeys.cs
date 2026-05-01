namespace ContentTours.Application.Caching;

public static class TourSearchCacheKeys
{
    public static string Search(string hash) => $"ct:tours:search:{hash}";

    public static string Suggest(string q, string? lang) =>
        $"ct:tours:suggest:{NormalizeQ(q)}:lang:{NormalizeLanguage(lang)}";

    public static string Featured(string? lang) =>
        $"ct:tours:featured:lang:{NormalizeLanguage(lang)}";

    public static string MyTours(Guid userId, int page, int pageSize, string? status, string? sort, bool includeDeleted = false) =>
        $"ct:my-tours:{userId}:p{page}:s{pageSize}:status:{status}:sort:{sort}:del:{includeDeleted}";

    /// <summary>
    /// Normalises an Accept-Language-shaped value or single language code to a stable
    /// cache-key segment. Strips quality-value suffixes, takes the first language only,
    /// trims, lowercases. Empty/null becomes <c>"default"</c>. Mirrors the convention
    /// used by ContentToursCacheKeys for tour reads (Phase-3 fix) so anonymous/admin
    /// callers with the same language land on the same suggest cache slot.
    /// </summary>
    private static string NormalizeLanguage(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return "default";
        var first = lang
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first)) return "default";
        return first
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
            .Trim()
            .ToLowerInvariant();
    }

    private static string NormalizeQ(string q) => (q ?? string.Empty).Trim().ToLowerInvariant();
}
