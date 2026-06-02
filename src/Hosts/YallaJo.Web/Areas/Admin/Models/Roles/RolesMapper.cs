using YallaJo.Web.Areas.Admin.Models.Roles;
using YallaJo.Web.Areas.Admin.Models.Roles;
using YallaJo.Web.Areas.Admin.Models.Roles;

namespace YallaJo.Web.Areas.Admin.Models.Roles;

internal static class RolesMapper
{
    public static RoleRowVm ToRowVm(RoleItemResponse r) => new()
    {
        Id          = r.Id,
        Name        = r.Name,
        Description = r.Description,
        IsActive    = r.IsActive,
    };

    public static CreateRoleRequest ToCreateRequest(CreateRoleVm vm) => new()
    {
        Name        = vm.Name.Trim(),
        Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
    };

    public static UpdateRoleRequest ToUpdateRequest(UpdateRoleVm vm) => new()
    {
        Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
    };

    public static AddRoleClaimRequest ToAddClaimRequest(AddRoleClaimVm vm) => new()
    {
        ClaimType  = vm.ClaimType.Trim(),
        ClaimValue = vm.ClaimValue.Trim(),
    };

    public static RoleClaimVm ToClaimVm(RoleClaimItemResponse c) => new()
    {
        Id         = c.Id,
        ClaimType  = c.ClaimType,
        ClaimValue = c.ClaimValue,
    };

    public static RoleDetailsVm ToDetailsVm(RoleDetailsResponse r) => new()
    {
        RoleId      = r.Id,
        Name        = r.Name,
        Description = r.Description,
        IsActive    = r.IsActive,
        UpdateVm    = new UpdateRoleVm { Description = r.Description },
        AddClaim    = new AddRoleClaimVm(),
        Claims      = r.Claims.Select(ToClaimVm).ToList(),
    };
}
