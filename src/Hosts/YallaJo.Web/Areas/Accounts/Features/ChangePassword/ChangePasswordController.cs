using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Features.ChangePassword.ViewModels;

namespace YallaJo.Web.Areas.Accounts.Features.ChangePassword;

[Area("Accounts")]
[Authorize]
public sealed class ChangePasswordController : Controller
{
    private readonly ChangePasswordFacade _facade;
    public ChangePasswordController(ChangePasswordFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index() => View(new ChangePasswordVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ChangePasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.HandleAsync(vm, ct);

        if (result.IsSuccess)
        {
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var msg in messages)
                    ModelState.AddModelError(field, msg);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not change password.");
        return View(vm);
    }
}
