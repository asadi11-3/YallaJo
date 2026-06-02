using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Areas.Admin.Models.Languages;

namespace YallaJo.Web.Areas.Admin.Models.Languages;

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
