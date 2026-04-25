using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events;

public sealed record BusinessStaffAddedDomainEvent(
    Guid StaffId,
    Guid BusinessId,
    Guid UserId,
    string Role) : DomainEventBase;
