using YallaJo.Web.Areas.Auth.Features.Login.Requests;
using YallaJo.Web.Areas.Auth.Features.Login.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.Login.Mappers;

public static class LoginMapper
{
    public static LoginRequest ToRequest(LoginVm vm) => new()
    {
        Email    = vm.Email.Trim(),
        Password = vm.Password,
    };
}
