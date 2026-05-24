using Booking.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetRefundPolicyByTour;

public sealed record GetRefundPolicyByTourQuery(Guid TourId)
    : IQuery<RefundPolicyDto>, ICacheableQuery
{
    public string CacheKey => BookingRefundPolicyCacheKeys.TourLookupKey(TourId);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

    public IReadOnlyList<string> Tags => [BookingRefundPolicyCacheKeys.TourTag(TourId)];
}
