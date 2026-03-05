
using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;


public sealed record PasswordChangedEvent(Guid UserId) : DomainEventBase;
