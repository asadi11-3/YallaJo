using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword;

[Area("Auth")]
[AllowAnonymous]
public sealed class ForgotPasswordController : Controller
{
    private readonly ForgotPasswordFacade _facade;
    public ForgotPasswordController(ForgotPasswordFacade facade) => _facade = facade;

    // GET /auth/forgotpassword
    [HttpGet]
    public IActionResult Index() => View(new ForgotPasswordVm());

    // POST /auth/forgotpassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ForgotPasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.HandleAsync(vm, ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "If that email is registered, a reset code has been sent.";
            return RedirectToAction("Index", "ResetPassword",
                new { area = "Auth", email = vm.Email });
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (f, msgs) in result.ValidationErrors)
                foreach (var m in msgs)
                    ModelState.AddModelError(f, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        return View(vm);
    }
}
