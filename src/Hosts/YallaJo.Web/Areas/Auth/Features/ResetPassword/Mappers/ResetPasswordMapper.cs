using YallaJo.Web.Areas.Auth.Features.ResetPassword.Requests;
using YallaJo.Web.Areas.Auth.Features.ResetPassword.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.ResetPassword.Mappers;

public static class ResetPasswordMapper
{
    public static ResetPasswordRequest ToRequest(ResetPasswordVm vm) => new()
    {
        Email              = vm.Email.Trim(),
        OtpCode            = vm.OtpCode.Trim(),
        NewPassword        = vm.NewPassword,
        ConfirmNewPassword = vm.ConfirmNewPassword,
    };
}
