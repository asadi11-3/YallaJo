using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Responses;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Mappers;

public static class SpecializationsMapper
{
    public static SpecializationRowVm ToRowVm(SpecializationItemResponse r) => new()
    {
        Id          = r.Id,
        Name        = r.Name,
        Description = r.Description,
        Icon        = r.Icon,
        IsActive    = r.IsActive,
    };

    public static CreateSpecializationRequest ToCreateRequest(CreateSpecializationVm vm) => new(
        Name:        vm.Name.Trim(),
        Description: string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        Icon:        string.IsNullOrWhiteSpace(vm.Icon) ? null : vm.Icon.Trim());

    public static UpdateSpecializationRequest ToUpdateRequest(UpdateSpecializationVm vm) => new(
        Name:        vm.Name.Trim(),
        Description: string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        Icon:        string.IsNullOrWhiteSpace(vm.Icon) ? null : vm.Icon.Trim(),
        IsActive:    vm.IsActive);
}
