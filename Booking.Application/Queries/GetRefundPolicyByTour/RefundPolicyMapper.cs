using Booking.Domain.Entities;
using Booking.Domain.ValueObjects;

namespace Booking.Application.Queries.GetRefundPolicyByTour;

internal static class RefundPolicyMapper
{
    public static RefundPolicyDto ToDto(RefundPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return new RefundPolicyDto(
            Id: policy.Id,
            TourId: policy.TourId,
            Tiers: policy.Tiers.Select(t => new RefundTierDto(t.HoursBeforeTour, t.RefundPercent)).ToList(),
            IsActive: policy.IsActive,
            IsDefault: false,
            RowVersion: Convert.ToBase64String(policy.RowVersion ?? []));
    }

    public static RefundPolicyDto ToDefaultDto(Guid tourId)
        => new(
            Id: Guid.Empty,
            TourId: tourId,
            Tiers: RefundPolicy.DefaultTiers
                .Select(t => new RefundTierDto(t.HoursBeforeTour, t.RefundPercent))
                .ToList(),
            IsActive: true,
            IsDefault: true,
            RowVersion: string.Empty);

    public static IEnumerable<RefundTier> ToDomainTiers(IEnumerable<RefundTierDto> tiers)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        return tiers.Select(t => new RefundTier(t.HoursBeforeTour, t.RefundPercent));
    }
}
