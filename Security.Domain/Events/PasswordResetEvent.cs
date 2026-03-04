using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;

public sealed record PasswordResetEvent(Guid UserId) : DomainEventBase;
