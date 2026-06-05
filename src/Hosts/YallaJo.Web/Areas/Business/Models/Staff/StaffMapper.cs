namespace YallaJo.Web.Areas.Business.Models.Staff;

public static class StaffMapper
{
    public static IReadOnlyList<StaffRowVm> ToRows(IReadOnlyList<BusinessStaffItemResponse> staff) =>
        staff
            .OrderBy(s => s.Role)
            .ThenBy(s => s.UserId)
            .Select(s => new StaffRowVm(s.Id, s.UserId, s.Role))
            .ToList();
}
