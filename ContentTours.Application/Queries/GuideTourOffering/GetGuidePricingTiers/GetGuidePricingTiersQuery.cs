using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuidePricingTiers;

public sealed record GuidePricingTierDto(
    Guid Id, string Name, string? Description, decimal Price, string Currency,
    int MinParticipants, int MaxParticipants, bool IsActive, DateTime CreatedAt);

public sealed record GetGuidePricingTiersQuery(Guid TourId, Guid TourGuideId)
    : IQuery<IReadOnlyList<GuidePricingTierDto>>, ICacheableQuery
{
    public string CacheKey => $"guide-pricing-tiers:{TourId}:{TourGuideId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForTourOfferings(TourId)];
}
