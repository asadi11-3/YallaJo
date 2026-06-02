using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class UpdatePhoneController : BaseController
{
    private readonly UpdatePhoneFacade _facade;
    public UpdatePhoneController(UpdatePhoneFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index() => View(new UpdatePhoneVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UpdatePhoneVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.HandleAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Phone number updated.");
            return RedirectToAction(nameof(Index));
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update phone number.");

        return View(vm);
    }
}
