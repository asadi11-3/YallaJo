namespace ContentTours.Contracts;

/// <summary>
/// Discriminator for ContentTours integration events that describe a mutation.
/// Used in <see cref="TourScheduleChangedIntegrationEvent"/> and
/// <see cref="TourPricingTierChangedIntegrationEvent"/> to give consumers a
/// compile-time-safe ChangeType instead of a freeform string.
/// </summary>
public enum TourEntityChangeType : byte
{
    Created     = 1,
    Updated     = 2,
    Deleted     = 3,
    Deactivated = 4
}
