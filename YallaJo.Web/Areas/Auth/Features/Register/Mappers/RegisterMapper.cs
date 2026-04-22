using YallaJo.Web.Areas.Auth.Features.Register.Requests;
using YallaJo.Web.Areas.Auth.Features.Register.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.Register.Mappers;

public static class RegisterMapper
{
    public static RegisterRequest ToRequest(RegisterVm vm) => new()
    {
        FirstName = vm.FirstName.Trim(),
        LastName  = vm.LastName.Trim(),
        Email     = vm.Email.Trim(),
        Password  = vm.Password,
        RecaptchaToken = vm.RecaptchaToken,
    };
}
