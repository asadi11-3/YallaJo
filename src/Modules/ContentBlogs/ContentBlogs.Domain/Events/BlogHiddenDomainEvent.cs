using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

/// <summary>
/// Raised when an admin hides a published article from public view.
/// </summary>
public sealed record BlogHiddenDomainEvent(
    Guid BlogId,
    Guid HiddenByAdminId,
    string Reason,
    DateTime HiddenAtUtc) : DomainEventBase;
