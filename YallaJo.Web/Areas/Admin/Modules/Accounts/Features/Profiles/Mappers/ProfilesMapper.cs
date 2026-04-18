using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.Requests;
using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.Mappers;

public static class ProfilesMapper
{
    public static CreateProfileRequest ToCreateRequest(CreateProfileVm vm) => new(
        UserId:      vm.UserId,
        FirstName:   vm.FirstName.Trim(),
        LastName:    vm.LastName.Trim(),
        DisplayName: string.IsNullOrWhiteSpace(vm.DisplayName) ? null : vm.DisplayName.Trim(),
        AvatarUrl:   string.IsNullOrWhiteSpace(vm.AvatarUrl) ? null : vm.AvatarUrl.Trim());
}
