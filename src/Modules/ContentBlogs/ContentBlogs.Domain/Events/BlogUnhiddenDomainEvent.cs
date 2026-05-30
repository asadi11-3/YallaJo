using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

/// <summary>
/// Raised when an admin restores a hidden article back to published.
/// </summary>
public sealed record BlogUnhiddenDomainEvent(
    Guid BlogId,
    DateTime UnhiddenAtUtc) : DomainEventBase;
