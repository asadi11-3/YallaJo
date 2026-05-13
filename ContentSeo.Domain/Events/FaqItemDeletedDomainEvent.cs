using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when a <see cref="Entities.FaqItem"/> is soft-deleted.
/// Outbox handler maps to <c>content-seo.faq-item.changed.v1</c>.
/// </summary>
public sealed record FaqItemDeletedDomainEvent(
    Guid FaqItemId,
    SeoEntityType EntityType,
    Guid EntityId) : DomainEventBase;
