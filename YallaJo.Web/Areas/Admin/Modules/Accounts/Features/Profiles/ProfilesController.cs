using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.ViewModels;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.User.Create)]
public sealed class ProfilesController : Controller
{
    private readonly ProfilesFacade _facade;
    public ProfilesController(ProfilesFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index() => View(new CreateProfileVm());

    [HttpPost("admin/profiles/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.Create)]
    public async Task<IActionResult> Create(CreateProfileVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(nameof(Index), vm);

        var result = await _facade.CreateAsync(vm, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (result.IsSuccess)
        {
            TempData["Success"] = $"Profile created (Id: {result.Data}).";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(nameof(Index), vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create profile.");
        return View(nameof(Index), vm);
    }
}
