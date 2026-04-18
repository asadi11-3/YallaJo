using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Features.UpdatePhone.ViewModels;

namespace YallaJo.Web.Areas.Accounts.Features.UpdatePhone;

[Area("Accounts")]
[Authorize]
public sealed class UpdatePhoneController : Controller
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

        if (result.IsSuccess)
        {
            TempData["Success"] = "Phone number updated.";
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

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update phone number.");
        return View(vm);
    }
}
