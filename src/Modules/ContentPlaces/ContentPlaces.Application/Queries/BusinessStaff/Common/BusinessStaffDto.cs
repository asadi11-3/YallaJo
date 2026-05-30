using ContentPlaces.Domain.Enums;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Application.Queries.BusinessStaff.Common;

public sealed record BusinessStaffDto(
    Guid Id,
    Guid BusinessId,
    Guid UserId,
    BusinessStaffRole Role)
{
    public static BusinessStaffDto From(StaffEntity staff) => new(
        staff.Id,
        staff.BusinessId,
        staff.UserId,
        staff.Role);
}
