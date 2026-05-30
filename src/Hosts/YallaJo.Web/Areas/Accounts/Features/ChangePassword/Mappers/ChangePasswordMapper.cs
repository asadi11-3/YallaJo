using YallaJo.Web.Areas.Accounts.Features.ChangePassword.Requests;
using YallaJo.Web.Areas.Accounts.Features.ChangePassword.ViewModels;

namespace YallaJo.Web.Areas.Accounts.Features.ChangePassword.Mappers;

public static class ChangePasswordMapper
{
    public static ChangePasswordRequest ToRequest(ChangePasswordVm vm) => new(
        CurrentPassword:    vm.CurrentPassword,
        NewPassword:        vm.NewPassword,
        ConfirmNewPassword: vm.ConfirmNewPassword);
}
