using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.Login.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.Login;

[Area("Auth")]
[AllowAnonymous]
public sealed class LoginController : Controller
{
    private readonly LoginFacade _facade;

    public LoginController(LoginFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _facade.HandleAsync(vm, ct);

        if (result.IsSuccess)
            return RedirectToLocal(vm.ReturnUrl);

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var msg in messages)
                    ModelState.AddModelError(field, msg);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
        return View(vm);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Sessions", new { area = "Auth" });
    }
}
