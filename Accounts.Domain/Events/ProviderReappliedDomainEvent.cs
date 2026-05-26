using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderReappliedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    int ReapplicationCount,
    DateTime ReappliedAt) : DomainEventBase;
