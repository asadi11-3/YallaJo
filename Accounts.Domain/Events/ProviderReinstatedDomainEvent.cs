using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderReinstatedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    DateTime ReinstatedAt) : DomainEventBase;
