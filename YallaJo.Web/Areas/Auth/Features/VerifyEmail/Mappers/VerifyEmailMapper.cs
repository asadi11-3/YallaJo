using YallaJo.Web.Areas.Auth.Features.VerifyEmail.Requests;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.VerifyEmail.Mappers;

public static class VerifyEmailMapper
{
    public static VerifyEmailRequest ToRequest(VerifyEmailVm vm) => new()
    {
        Email   = vm.Email.Trim(),
        OtpCode = vm.OtpCode.Trim(),
        RecaptchaToken = vm.RecaptchaToken,
    };
}
