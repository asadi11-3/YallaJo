using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when a new <see cref="Entities.FaqItem"/> is created.
/// Outbox handler maps to <c>content-seo.faq-item.changed.v1</c>.
/// </summary>
public sealed record FaqItemCreatedDomainEvent(
    Guid FaqItemId,
    SeoEntityType EntityType,
    Guid EntityId) : DomainEventBase;
