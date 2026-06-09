using YallaJo.Web.Areas.Admin.Models.Home;

namespace YallaJo.Web.Areas.Admin.Models.Statistics;

public static class StatisticsMapper
{
    public static StatisticsVm ToVm(
        InteractionPageResponse page,
        string? userLookupId = null,
        bool isUserLookup = false,
        IReadOnlyDictionary<Guid, string>? entityNames = null,
        IReadOnlyDictionary<Guid, string>? userNames = null)
    {
        var rows = page.Items
            .Select(x => new InteractionRowVm
            {
                Id = x.Id,
                UserId = x.UserId,
                InteractionType = x.InteractionType,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                OccurredAt = x.OccurredAt,
                UserAgent = x.UserAgent,
                EntityName = entityNames is not null && entityNames.TryGetValue(x.EntityId, out var en) ? en : null,
                UserName = x.UserId is { } uid && userNames is not null && userNames.TryGetValue(uid, out var un) ? un : null
            })
            .ToList();

        var breakdown = rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.InteractionType) ? "unknown" : r.InteractionType)
            .Select(g => new InteractionTypeCountVm { InteractionType = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        return new StatisticsVm
        {
            TypeBreakdown = breakdown,
            Recent = rows,
            TotalShown = rows.Count,
            UserLookupId = userLookupId,
            IsUserLookup = isUserLookup
        };
    }
}
