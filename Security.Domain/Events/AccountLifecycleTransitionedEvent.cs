using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;

public sealed record AccountLifecycleTransitionedEvent(
    Guid UserId,
    AccountLifecycleState From,
    AccountLifecycleState To) : DomainEventBase;
