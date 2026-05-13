using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when a <see cref="Entities.FaqItem"/>'s <c>SortOrder</c> changes,
/// typically via a bulk drag-and-drop reorder of the FAQ list.
/// Outbox handler maps to <c>content-seo.faq-item.changed.v1</c>.
/// </summary>
public sealed record FaqItemReorderedDomainEvent(
    Guid FaqItemId,
    SeoEntityType EntityType,
    Guid EntityId,
    int OldSortOrder,
    int NewSortOrder) : DomainEventBase;
