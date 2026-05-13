using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when an <see cref="Entities.SeoMetadata"/> record is created for an entity.
/// Outbox handler maps to <c>content-seo.seo-metadata.changed.v1</c>.
/// </summary>
public sealed record SeoMetadataCreatedDomainEvent(
    Guid SeoMetadataId,
    SeoEntityType EntityType,
    Guid EntityId) : DomainEventBase;
