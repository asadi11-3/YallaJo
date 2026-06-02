using YallaJo.Web.Areas.Auth.Models.Login;

namespace YallaJo.Web.Areas.Auth.Models.Login;

public static class LoginMapper
{
    public static LoginRequest ToRequest(LoginVm vm) => new()
    {
        Email    = vm.Email.Trim(),
        Password = vm.Password,
        RecaptchaToken = vm.RecaptchaToken,
    };
}
