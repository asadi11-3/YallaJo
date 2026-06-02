using YallaJo.Web.Areas.Auth.Models.ForgotPassword;

namespace YallaJo.Web.Areas.Auth.Models.ForgotPassword;

public static class ForgotPasswordMapper
{
    public static ForgotPasswordRequest ToRequest(ForgotPasswordVm vm) => new()
    {
        Email = vm.Email.Trim(),
        RecaptchaToken = vm.RecaptchaToken,
    };
}
