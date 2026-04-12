using Microsoft.AspNetCore.Mvc.ModelBinding;
using YallaJo.Web.Areas.Auth.Features.Login.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.Login.Validators;

public static class LoginVmValidator
{
    public static bool IsValid(LoginVm vm, ModelStateDictionary modelState)
        => modelState.IsValid;
}
