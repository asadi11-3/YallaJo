using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.BusinessStaff;

public sealed record BusinessStaffAddedIntegrationEvent(
    Guid StaffId,
    Guid BusinessId,
    Guid UserId,
    string Role) : IntegrationEventBase;
