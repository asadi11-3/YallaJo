using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;

public sealed record UserCreatedEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : DomainEventBase;
