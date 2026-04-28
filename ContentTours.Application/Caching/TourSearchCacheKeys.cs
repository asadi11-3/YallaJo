namespace ContentTours.Application.Caching;

public static class TourSearchCacheKeys
{
    public static string Search(string hash) => $"ct:tours:search:{hash}";
    public static string Suggest(string q, string lang) => $"ct:tours:suggest:{q}:lang:{lang}";
    public static string Featured(string lang) => $"ct:tours:featured:lang:{lang}";
    public static string MyTours(Guid userId, int page, int pageSize, string? status, string? sort, bool includeDeleted = false) =>
        $"ct:my-tours:{userId}:p{page}:s{pageSize}:status:{status}:sort:{sort}:del:{includeDeleted}";
}
