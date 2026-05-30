using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogUnfeaturedIntegrationEvent(
    Guid BlogId,
    string Slug,
    Guid? PlaceId,
    DateTime UnfeaturedAt) : IntegrationEventBase;
