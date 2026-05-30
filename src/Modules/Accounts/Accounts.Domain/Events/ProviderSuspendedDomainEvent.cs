using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderSuspendedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    string Reason,
    DateTime SuspendedAt) : DomainEventBase;
