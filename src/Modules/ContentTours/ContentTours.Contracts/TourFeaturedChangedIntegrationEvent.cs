using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

/// <summary>
/// Raised when admin toggles IsFeatured on a Tour.
/// Consumers: Analytics (log editorial curation), ContentSeo (boost in sitemap priority).
/// </summary>
public sealed record TourFeaturedChangedIntegrationEvent(
    Guid TourId,
    bool IsFeatured,
    Guid ChangedByUserId,
    DateTime ChangedAt
) : IntegrationEventBase;
