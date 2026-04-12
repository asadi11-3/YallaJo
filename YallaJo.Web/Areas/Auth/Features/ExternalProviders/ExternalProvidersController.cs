using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

[Area("Auth")]
[Authorize]
public sealed class ExternalProvidersController : Controller
{
    private readonly ExternalProvidersFacade _facade;
    public ExternalProvidersController(ExternalProvidersFacade facade) => _facade = facade;

    // GET /auth/externalproviders
    [HttpGet]
    public IActionResult Index() => View(new ExternalProvidersVm());

    // POST /auth/externalproviders (link)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ExternalProvidersVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.LinkAsync(vm, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (result.IsSuccess)
        {
            vm.Message = result.Message;
            return View(vm);
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

    // POST /auth/externalproviders/unlink/{providerId}
    [HttpPost("auth/externalproviders/unlink/{providerId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlink(Guid providerId, CancellationToken ct)
    {
        var result = await _facade.UnlinkAsync(providerId, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        TempData["ProviderMessage"] = result.IsSuccess ? result.Message : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
