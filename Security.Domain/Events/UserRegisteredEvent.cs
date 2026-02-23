using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Email) : DomainEventBase;
