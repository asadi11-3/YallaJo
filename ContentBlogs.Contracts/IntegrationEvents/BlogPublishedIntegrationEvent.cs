using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogPublishedIntegrationEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? PlaceId,
    DateTime PublishedAt,
    string CanonicalUrl = "") : IntegrationEventBase;
