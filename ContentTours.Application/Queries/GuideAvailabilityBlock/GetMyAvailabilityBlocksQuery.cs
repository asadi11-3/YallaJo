using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.GuideAvailabilityBlock;

public sealed record GuideAvailabilityBlockDto(
    Guid Id, DateOnly StartDate, DateOnly EndDate, string? Reason, DateTime CreatedAt);

public sealed record GetMyAvailabilityBlocksQuery(Guid GuideUserId)
    : IQuery<IReadOnlyList<GuideAvailabilityBlockDto>>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.AvailabilityBlocks(GuideUserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForAvailabilityBlocks(GuideUserId)];
}
