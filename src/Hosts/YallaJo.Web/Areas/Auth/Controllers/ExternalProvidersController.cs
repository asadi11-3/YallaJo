using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Models.ExternalProviders;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Areas.Auth.Facades;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[Authorize]
public sealed class ExternalProvidersController : BaseController
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

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Provider unlinked.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }
}
