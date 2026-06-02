using YallaJo.Web.Areas.Auth.Models.VerifyEmail;

namespace YallaJo.Web.Areas.Auth.Models.VerifyEmail;

public static class VerifyEmailMapper
{
    public static VerifyEmailRequest ToRequest(VerifyEmailVm vm) => new()
    {
        Email   = vm.Email.Trim(),
        OtpCode = vm.OtpCode.Trim(),
        RecaptchaToken = vm.RecaptchaToken,
    };
}
