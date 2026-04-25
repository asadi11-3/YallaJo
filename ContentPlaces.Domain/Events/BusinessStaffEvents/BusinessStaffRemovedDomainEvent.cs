using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events.BusinessStaffEvents;

public sealed record BusinessStaffRemovedDomainEvent(
    Guid StaffId,
    Guid BusinessId,
    Guid UserId) : DomainEventBase;
