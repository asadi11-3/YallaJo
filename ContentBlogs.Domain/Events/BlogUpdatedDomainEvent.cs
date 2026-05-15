using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogUpdatedDomainEvent(
    Guid BlogId,
    string OldSlug,
    string NewSlug,
    IReadOnlyList<string> FieldsChanged,
    DateTime UpdatedAtUtc) : DomainEventBase;
