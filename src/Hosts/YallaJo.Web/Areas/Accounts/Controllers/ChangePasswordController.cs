using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class ChangePasswordController : BaseController
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
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Password changed successfully.");
            return RedirectToAction(nameof(Index));
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not change password.");

        return View(vm);
    }
}
