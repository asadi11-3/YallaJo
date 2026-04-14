using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Requests;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Responses;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Mappers;

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
}
