using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderRejectedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    string Reason,
    DateTime RejectedAt) : DomainEventBase;
