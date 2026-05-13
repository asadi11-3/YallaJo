using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when a <see cref="Entities.SeoMetadata"/> record is soft-deleted.
/// Outbox handler maps to <c>content-seo.seo-metadata.changed.v1</c>.
/// </summary>
public sealed record SeoMetadataDeletedDomainEvent(
    Guid SeoMetadataId,
    SeoEntityType EntityType,
    Guid EntityId) : DomainEventBase;
