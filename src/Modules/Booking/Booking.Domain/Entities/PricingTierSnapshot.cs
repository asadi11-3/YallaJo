using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

/// <summary>
/// Booking-owned denormalized snapshot of a Tour's pricing tier.
/// Populated via inbox handlers listening to ContentTours TourPricingTierChanged events.
/// </summary>
public sealed class PricingTierSnapshot : BaseEntity
{
    private PricingTierSnapshot()
    {
    }

    public Guid TourId { get; private set; }
    public TierType TierType { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime LastUpdatedAt { get; private set; }

    public static PricingTierSnapshot Create(
        Guid tourId,
        TierType tierType,
        decimal price,
        string currency)
    {
        return new PricingTierSnapshot
        {
            TourId = tourId,
            TierType = tierType,
            Price = price,
            Currency = currency,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(decimal price, string currency)
    {
        Price = price;
        Currency = currency;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
