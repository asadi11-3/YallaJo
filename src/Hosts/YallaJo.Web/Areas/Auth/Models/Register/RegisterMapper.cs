using YallaJo.Web.Areas.Auth.Models.Register;

namespace YallaJo.Web.Areas.Auth.Models.Register;

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
