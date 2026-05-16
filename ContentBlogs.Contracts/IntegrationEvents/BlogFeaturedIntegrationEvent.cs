using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogFeaturedIntegrationEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? PlaceId,
    DateTime FeaturedAt) : IntegrationEventBase;
