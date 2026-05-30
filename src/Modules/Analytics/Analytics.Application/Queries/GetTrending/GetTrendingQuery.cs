using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetTrending;

public sealed record GetTrendingQuery(int Count = 20) : IQuery<IReadOnlyList<PopularEntityDto>>, ICacheableQuery
{
    public string CacheKey => $"trending:{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["trending"];
}
