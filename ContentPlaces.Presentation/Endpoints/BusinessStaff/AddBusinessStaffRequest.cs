using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Presentation.Endpoints.BusinessStaff;

public sealed record AddBusinessStaffRequest(
    Guid UserId,
    BusinessStaffRole Role);
