using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when an existing <see cref="Entities.FaqItem"/>'s question/answer/order changes.
/// Outbox handler maps to <c>content-seo.faq-item.changed.v1</c>.
/// </summary>
public sealed record FaqItemUpdatedDomainEvent(
    Guid FaqItemId,
    SeoEntityType EntityType,
    Guid EntityId) : DomainEventBase;
