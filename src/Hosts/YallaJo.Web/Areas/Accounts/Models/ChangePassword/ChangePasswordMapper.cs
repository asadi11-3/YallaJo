using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Areas.Accounts.Models.ChangePassword;

namespace YallaJo.Web.Areas.Accounts.Models.ChangePassword;

public static class ChangePasswordMapper
{
    public static ChangePasswordRequest ToRequest(ChangePasswordVm vm) => new(
        CurrentPassword:    vm.CurrentPassword,
        NewPassword:        vm.NewPassword,
        ConfirmNewPassword: vm.ConfirmNewPassword);
}
