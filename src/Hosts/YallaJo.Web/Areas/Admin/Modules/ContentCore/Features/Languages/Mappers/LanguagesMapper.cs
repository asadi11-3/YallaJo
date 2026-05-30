using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Responses;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Mappers;

public static class LanguagesMapper
{
    public static LanguageRowVm ToRowVm(LanguageItemResponse r) => new()
    {
        Id         = r.Id,
        Code       = r.Code,
        Name       = r.Name,
        NativeName = r.NativeName,
        IsRtl      = r.IsRtl,
        IsActive   = r.IsActive,
    };

    public static CreateLanguageRequest ToCreateRequest(CreateLanguageVm vm) => new(
        Code:       vm.Code.Trim(),
        Name:       vm.Name.Trim(),
        NativeName: vm.NativeName.Trim(),
        IsRtl:      vm.IsRtl);

    public static UpdateLanguageRequest ToUpdateRequest(UpdateLanguageVm vm) => new(
        Name:       vm.Name.Trim(),
        NativeName: vm.NativeName.Trim(),
        IsRtl:      vm.IsRtl,
        IsActive:   vm.IsActive);
}
