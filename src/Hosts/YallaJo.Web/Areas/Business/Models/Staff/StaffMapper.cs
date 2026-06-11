namespace YallaJo.Web.Areas.Business.Models.Staff;

public static class StaffMapper
{
    public static IReadOnlyList<StaffRowVm> ToRows(IReadOnlyList<BusinessStaffItemResponse> staff) =>
        ToRows(staff, lookup: null);

    /// <summary>Maps rows, enriching identity from a B1 lookup result when available (D-5).</summary>
    public static IReadOnlyList<StaffRowVm> ToRows(
        IReadOnlyList<BusinessStaffItemResponse> staff,
        IReadOnlyDictionary<Guid, UserLookupItemResponse>? lookup) =>
        staff
            .OrderBy(s => s.Role)
            .ThenBy(s => s.UserId)
            .Select(s =>
            {
                UserLookupItemResponse? user = null;
                lookup?.TryGetValue(s.UserId, out user);
                return new StaffRowVm(s.Id, s.UserId, s.Role, user?.DisplayName, user?.Email);
            })
            .ToList();
}
