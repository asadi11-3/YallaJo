using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.BusinessStaff;

public sealed record BusinessStaffRemovedIntegrationEvent(
    Guid StaffId,
    Guid BusinessId,
    Guid UserId) : IntegrationEventBase;
