using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when an existing <see cref="Entities.Redirect"/> is deactivated
/// (taken out of the active redirect table without being deleted).
/// </summary>
public sealed record RedirectDeactivatedDomainEvent(
    Guid RedirectId,
    string OldUrl) : DomainEventBase;
