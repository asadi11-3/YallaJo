using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Models.ExternalProviders;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Infrastructure.Authentication.Claims;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Infrastructure.Security.Recaptcha;
using YallaJo.Web.Areas.Auth.Facades;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[AllowAnonymous]
[Route("auth/external")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // C2: interstitial carries a one-time auth ticket
public sealed class ExternalAuthController : BaseController
{
    private readonly ExternalProvidersFacade _facade;
    private readonly IExternalProviderAvailability _availability;
    private readonly ILogger<ExternalAuthController> _logger;
    private readonly IStringLocalizer<YallaJo.Web.Resources.SharedResource> _localizer;

    private const string ModeItemKey = "ExternalAuth.Mode";
    private const string ReturnUrlItemKey = "ExternalAuth.ReturnUrl";
    private const string OwnerUserItemKey = "ExternalAuth.OwnerUserId";
    private const string CompleteViewPath =
        "~/Areas/Auth/Views/ExternalProviders/Complete.cshtml";

    public ExternalAuthController(
        ExternalProvidersFacade facade,
        IExternalProviderAvailability availability,
        ILogger<ExternalAuthController> logger,
        IStringLocalizer<YallaJo.Web.Resources.SharedResource> localizer)
    {
        _facade = facade;
        _availability = availability;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpPost("challenge")]
    [ValidateAntiForgeryToken]
    public IActionResult Challenge([FromForm] string provider, [FromForm] string? returnUrl, [FromForm] string mode)
    {
        if (!IsProviderAvailable(provider))
            return BadRequest(_localizer["Auth.External.ProviderNotConfigured"].Value); // CON1

        var safeReturn = NormalizeReturnUrl(returnUrl);
        var safeMode = string.Equals(mode, "link", StringComparison.OrdinalIgnoreCase) ? "link" : "login";

        if (safeMode == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var ownerId = User.FindFirstValue(AppClaimTypes.UserId);
            if (string.IsNullOrWhiteSpace(ownerId))
                return Challenge();

            var linkProps = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), "ExternalAuth",
                    new { area = "Auth", provider, mode = safeMode })!,
                AllowRefresh = false,
            };
            linkProps.Items[ModeItemKey] = safeMode;
            linkProps.Items[ReturnUrlItemKey] = safeReturn;
            linkProps.Items[OwnerUserItemKey] = ownerId;

            return Challenge(linkProps, provider);
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback), "ExternalAuth",
                new { area = "Auth", provider, mode = safeMode })!,
            AllowRefresh = false,
        };
        props.Items[ModeItemKey] = safeMode;
        props.Items[ReturnUrlItemKey] = safeReturn;

        return Challenge(props, provider);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string provider, string? mode)
    {
        if (!IsProviderAvailable(provider))
            return BadRequest(_localizer["Auth.External.ProviderNotConfigured"].Value); // CON1

        var authResult = await HttpContext.AuthenticateAsync(
            ExternalProviderConstants.ExternalSignInScheme);

        await HttpContext.SignOutAsync(ExternalProviderConstants.ExternalSignInScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            _logger.LogInformation("External auth callback for {Provider} failed (no principal).", provider);
            return RedirectToLogin();
        }

        var items = authResult.Properties?.Items ?? new Dictionary<string, string?>();
        var storedMode = items.TryGetValue(ModeItemKey, out var m) ? m : mode;
        var storedReturn = NormalizeReturnUrl(items.TryGetValue(ReturnUrlItemKey, out var r) ? r : null);

        var providerUserId = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);

        var emailVerifiedClaim = authResult.Principal.FindFirstValue(
            ExternalAuthTicketClaims.EmailVerified);
        var emailVerified = bool.TryParse(emailVerifiedClaim, out var ev) && ev;

        var firstName = authResult.Principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = authResult.Principal.FindFirstValue(ClaimTypes.Surname);

        _logger.LogInformation(
            "External auth callback: Provider={Provider} ProviderUserId={ProviderUserId} HasEmail={HasEmail} EmailVerifiedClaim={EmailVerifiedClaim} ParsedEmailVerified={ParsedEmailVerified} Mode={Mode}",
            provider,
            providerUserId ?? "(null)",
            !string.IsNullOrWhiteSpace(email),
            emailVerifiedClaim ?? "(claim-missing)",
            emailVerified,
            mode ?? "(null)");

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            _logger.LogWarning("External auth callback for {Provider} missing NameIdentifier claim.", provider);
            return RedirectToLogin();
        }

        var safeMode = string.Equals(storedMode, "link", StringComparison.OrdinalIgnoreCase) ? "link" : "login";

        if (safeMode == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                SetError(_localizer["Auth.Flash.SignInFirstToLink"].Value);
                return RedirectToLogin();
            }

            var currentUser = User.FindFirstValue(AppClaimTypes.UserId);
            var ownerAtChallenge = items.TryGetValue(OwnerUserItemKey, out var owner) ? owner : null;
            if (!string.IsNullOrWhiteSpace(ownerAtChallenge)
                && !string.Equals(ownerAtChallenge, currentUser, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "External provider link aborted: signed-in user {Current} differs from challenge owner {Owner}.",
                    currentUser, ownerAtChallenge);
                return RedirectToAction("Index", "ExternalProviders", new { area = "Auth" });
            }
        }

        var ticket = _facade.BuildTicket(
            provider, providerUserId, email, emailVerified, firstName, lastName);

        var vm = new ExternalAuthCompleteVm
        {
            Provider = provider,
            Ticket = ticket,
            Mode = safeMode,
            ReturnUrl = storedReturn,
            RecaptchaAction = safeMode == "link"
                ? RecaptchaActions.LinkProvider
                : RecaptchaActions.ExternalLogin,
        };

        return View(CompleteViewPath, vm);
    }

    [HttpPost("complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(
        [FromForm] string provider,
        [FromForm] string ticket,
        [FromForm] string mode,
        [FromForm] string? returnUrl,
        [FromForm(Name = "RecaptchaToken")] string recaptchaToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return RedirectToLogin();

        var safeReturn = NormalizeReturnUrl(returnUrl);
        var safeMode = string.Equals(mode, "link", StringComparison.OrdinalIgnoreCase) ? "link" : "login";

        if (safeMode == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                SetError(_localizer["Auth.Flash.SignInFirstToLink"].Value);
                return RedirectToLogin();
            }

            var linkResult = await _facade.LinkAsync(ticket, recaptchaToken ?? string.Empty, ct);

            if (linkResult.IsSuccess)
            {
                SetSuccess(_localizer["Auth.Flash.ProviderLinked", provider].Value);
            }
            else
            {
                // There is no form to re-render in the ticket flow, so field-level
                // validation errors (if any) are collapsed into the flash message
                // instead of being dropped (linkResult.Error is null for the
                // Invalid case, which previously lost the reason entirely).
                var firstValidation = linkResult.ValidationErrors?
                    .SelectMany(kv => kv.Value)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
                SetError(linkResult.Error ?? firstValidation ?? _localizer["Auth.Flash.ProviderLinkFailed"].Value);
            }

            // Flash is written before the guard on purpose: the original behavior
            // bounced to sign-in with the message intact when the session expired.
            if (GuardSignOut(linkResult) is { } signOut) return signOut;

            return RedirectToAction("Index", "ExternalProviders", new { area = "Auth" });
        }

        var outcome = await _facade.LoginAsync(ticket, recaptchaToken ?? string.Empty, ct);

        if (outcome.IsSuccess)
            return RedirectLocal(safeReturn);

        // Standard _Alerts flash (was a bespoke TempData["LoginError"] side channel).
        // outcome.Error is always facade-authored friendly copy — never a raw
        // provider/API error — so it is safe to surface verbatim.
        SetError(outcome.Error ?? _localizer["Auth.Flash.ExternalSignInFailed"].Value);
        return RedirectToLogin();
    }

    private bool IsProviderAvailable(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider)) return false;
        var norm = provider.Trim().ToLowerInvariant();
        return _availability.AvailableProviders.Any(p =>
            string.Equals(p, norm, StringComparison.Ordinal));
    }

    private string? NormalizeReturnUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return Url.IsLocalUrl(raw) ? raw : null;
    }

    private IActionResult RedirectLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Sessions", new { area = "Auth" });
    }
}
