using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;

public sealed record PhoneNumberUpdatedEvent(
    Guid UserId,
    string PhoneNumber,
    bool IsPrimary) : DomainEventBase;
