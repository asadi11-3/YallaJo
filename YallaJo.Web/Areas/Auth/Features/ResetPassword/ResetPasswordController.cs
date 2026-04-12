using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.ResetPassword.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.ResetPassword;

[Area("Auth")]
[AllowAnonymous]
public sealed class ResetPasswordController : Controller
{
    private readonly ResetPasswordFacade _facade;
    public ResetPasswordController(ResetPasswordFacade facade) => _facade = facade;

    // GET /auth/resetpassword
    [HttpGet]
    public IActionResult Index(string? email = null) =>
        View(new ResetPasswordVm { Email = email ?? string.Empty });

    // POST /auth/resetpassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ResetPasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.HandleAsync(vm, ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Password reset successfully. Please sign in.";
            return RedirectToAction("Index", "Login", new { area = "Auth" });
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
