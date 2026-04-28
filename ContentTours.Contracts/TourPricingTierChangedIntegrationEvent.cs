using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

/// <summary>
/// Raised on Create/Update/Delete/Deactivate of a TourPricingTier.
/// Consumers: Finance (recompute Tour.SalePrice if discount active), Analytics (price-change tracking).
/// </summary>
public sealed record TourPricingTierChangedIntegrationEvent(
    Guid TierId,
    Guid TourId,
    /// <summary>Last-known price. On Deleted events this is the price at time of deletion.</summary>
    decimal Price,
    string Currency,
    TourEntityChangeType ChangeType
) : IntegrationEventBase;
