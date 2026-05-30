using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when an existing <see cref="Entities.SeoMetadata"/> record's meta fields change.
/// Outbox handler maps to <c>content-seo.seo-metadata.changed.v1</c>.
/// </summary>
public sealed record SeoMetadataUpdatedDomainEvent(
    Guid SeoMetadataId,
    SeoEntityType EntityType,
    Guid EntityId) : DomainEventBase;
