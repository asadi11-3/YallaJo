using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Auth.Models.ExternalProviders;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Areas.Auth.Facades;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // C2: auth-gated account page
public sealed class ExternalProvidersController : BaseController
{
    private readonly ExternalProvidersFacade _facade;
    private readonly IExternalProviderAvailability _availability;
    private readonly IStringLocalizer<YallaJo.Web.Resources.SharedResource> _localizer;

    public ExternalProvidersController(
        ExternalProvidersFacade facade,
        IExternalProviderAvailability availability,
        IStringLocalizer<YallaJo.Web.Resources.SharedResource> localizer)
    {
        _facade = facade;
        _availability = availability;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var (vm, signOut) = await BuildVmAsync(ct);

        if (signOut is not null)
            return signOut;

        return WantsAjax() ? PartialView("_ProvidersList", vm) : View(vm);
    }

    [HttpPost("auth/externalproviders/unlink/{providerId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlink(Guid providerId, CancellationToken ct)
    {
        var result = await _facade.UnlinkAsync(providerId, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
        {
            // B6 lockout guard — show the localized explanation rather than relaying
            // the API's English message.
            var error = result.IsConflict
                ? _localizer["Auth.External.LastLoginMethod"].Value
                : result.Error ?? _localizer["Auth.External.ActionFailed"].Value;

            if (WantsAjax())
                return BadRequest(new { error });

            SetError(error);
            return RedirectToAction(nameof(Index));
        }

        if (WantsAjax())
        {
            // PE1: AJAX callers get the refreshed partial; the toast text travels on
            // the form's data-success-msg attribute.
            var (vm, _) = await BuildVmAsync(ct);
            return PartialView("_ProvidersList", vm);
        }

        SetSuccess(_localizer["Auth.Flash.ProviderUnlinked"].Value);
        return RedirectToAction(nameof(Index));
    }

    private async Task<(ExternalProvidersVm Vm, IActionResult? SignOut)> BuildVmAsync(CancellationToken ct)
    {
        var vm = new ExternalProvidersVm
        {
            IsGoogleAvailable = _availability.IsGoogleAvailable,
            IsFacebookAvailable = _availability.IsFacebookAvailable,
        };

        var linked = await _facade.GetLinkedAsync(ct);

        if (GuardSignOut(linked) is { } signOut)
            return (vm, signOut);

        if (linked.IsSuccess && linked.Data is not null)
        {
            vm.LinkedProviders = linked.Data;
        }
        else
        {
            // Safe degrade: the page still renders the link buttons; the view shows
            // an inline warning that current link state could not be loaded.
            vm.LinkedListUnavailable = true;
        }

        return (vm, null);
    }
}
