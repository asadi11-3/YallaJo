using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderApplicationSubmittedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    DateTime SubmittedAt) : DomainEventBase;
