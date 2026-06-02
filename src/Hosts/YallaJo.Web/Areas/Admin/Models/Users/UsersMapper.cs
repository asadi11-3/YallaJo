using YallaJo.Web.Areas.Admin.Models.Users;
using YallaJo.Web.Areas.Admin.Models.Users;
using YallaJo.Web.Areas.Admin.Models.Users;

namespace YallaJo.Web.Areas.Admin.Models.Users;

internal static class UsersMapper
{
    public static UserRowVm ToRowVm(UserItemResponse r) => new()
    {
        Id       = r.Id,
        Email    = r.Email,
        IsActive = r.IsActive,
        Roles    = r.Roles,
    };

    public static AssignRoleRequest ToAssignRoleRequest(AssignRoleVm vm) => new()
    {
        // vm.RoleId is guaranteed non-null here: [Required] already rejected null
        // before the controller reaches this mapper call.
        RoleId = vm.RoleId!.Value,
    };

    public static AddUserClaimRequest ToAddClaimRequest(AddClaimVm vm) => new()
    {
        ClaimType  = vm.ClaimType.Trim(),
        ClaimValue = vm.ClaimValue.Trim(),
    };
}
