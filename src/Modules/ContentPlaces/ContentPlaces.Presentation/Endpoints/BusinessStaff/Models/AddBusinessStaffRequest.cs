using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Presentation.Endpoints.BusinessStaff.Models;

public sealed record AddBusinessStaffRequest(
    Guid UserId,
    BusinessStaffRole Role);
