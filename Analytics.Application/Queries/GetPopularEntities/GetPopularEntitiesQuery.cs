using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetPopularEntities;

public sealed record GetPopularEntitiesQuery(string EntityType, int Count = 20) : IQuery<IReadOnlyList<PopularEntityDto>>, ICacheableQuery
{
    public string CacheKey => $"popular:{EntityType}:{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [$"popular:{EntityType}"];
}
