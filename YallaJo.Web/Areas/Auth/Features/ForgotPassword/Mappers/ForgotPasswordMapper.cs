using YallaJo.Web.Areas.Auth.Features.ForgotPassword.Requests;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword.Mappers;

public static class ForgotPasswordMapper
{
    public static ForgotPasswordRequest ToRequest(ForgotPasswordVm vm) => new()
    {
        Email = vm.Email.Trim(),
    };
}
