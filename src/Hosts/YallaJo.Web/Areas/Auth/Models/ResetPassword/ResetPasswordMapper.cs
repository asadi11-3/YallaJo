using YallaJo.Web.Areas.Auth.Models.ResetPassword;

namespace YallaJo.Web.Areas.Auth.Models.ResetPassword;

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
