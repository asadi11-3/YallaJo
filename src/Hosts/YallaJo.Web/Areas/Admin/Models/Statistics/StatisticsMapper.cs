using YallaJo.Web.Areas.Admin.Models.Home;

namespace YallaJo.Web.Areas.Admin.Models.Statistics;

public static class StatisticsMapper
{
    public static StatisticsVm ToVm(
        InteractionPageResponse page,
        string? userLookupId = null,
        bool isUserLookup = false)
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
                UserAgent = x.UserAgent
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
