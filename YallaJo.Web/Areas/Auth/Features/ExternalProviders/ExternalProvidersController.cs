using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

/// <summary>
/// Manages the linked-providers UI for the signed-in user. Linking itself is
/// no longer done by form POST — it MUST flow through a real OAuth challenge
/// at <c>/auth/external/challenge</c> because only a verified provider
/// identity is accepted by the API. This controller is therefore only a
/// presentation shell for the list + unlink actions.
/// </summary>
[Area("Auth")]
[Authorize]
public sealed class ExternalProvidersController : Controller
{
    private readonly ExternalProvidersFacade _facade;
    private readonly IExternalProviderAvailability _availability;

    public ExternalProvidersController(
        ExternalProvidersFacade facade,
        IExternalProviderAvailability availability)
    {
        _facade = facade;
        _availability = availability;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var vm = new ExternalProvidersVm
        {
            Message = TempData["ProviderMessage"] as string,
            IsGoogleAvailable = _availability.IsGoogleAvailable,
            IsFacebookAvailable = _availability.IsFacebookAvailable,
        };
        return View(vm);
    }

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
