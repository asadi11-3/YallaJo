using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Models.ExternalProviders;
using YallaJo.Web.Infrastructure.Authentication.Claims;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Security.Recaptcha;
using YallaJo.Web.Areas.Auth.Facades;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[AllowAnonymous]
[Route("auth/external")]
public sealed class ExternalAuthController : Controller
{
    private readonly ExternalProvidersFacade _facade;
    private readonly IExternalProviderAvailability _availability;
    private readonly ILogger<ExternalAuthController> _logger;

    private const string ModeItemKey = "ExternalAuth.Mode";
    private const string ReturnUrlItemKey = "ExternalAuth.ReturnUrl";
    private const string OwnerUserItemKey = "ExternalAuth.OwnerUserId";
    private const string CompleteViewPath =
        "~/Areas/Auth/Views/ExternalProviders/Complete.cshtml";

    public ExternalAuthController(
        ExternalProvidersFacade facade,
        IExternalProviderAvailability availability,
        ILogger<ExternalAuthController> logger)
    {
        _facade = facade;
        _availability = availability;
        _logger = logger;
    }

    [HttpPost("challenge")]
    [ValidateAntiForgeryToken]
    public IActionResult Challenge([FromForm] string provider, [FromForm] string? returnUrl, [FromForm] string mode)
    {
        if (!IsProviderAvailable(provider))
            return BadRequest("Provider is not configured.");

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
            return BadRequest("Provider is not configured.");

        var authResult = await HttpContext.AuthenticateAsync(
            ExternalProviderConstants.ExternalSignInScheme);

        await HttpContext.SignOutAsync(ExternalProviderConstants.ExternalSignInScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            _logger.LogInformation("External auth callback for {Provider} failed (no principal).", provider);
            return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
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
            return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
        }

        var safeMode = string.Equals(storedMode, "link", StringComparison.OrdinalIgnoreCase) ? "link" : "login";

        if (safeMode == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["Error"] = "Please sign in first, then link your account.";
                return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
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
            return RedirectToAction("SignIn", "Auth", new { area = "Auth" });

        var safeReturn = NormalizeReturnUrl(returnUrl);
        var safeMode = string.Equals(mode, "link", StringComparison.OrdinalIgnoreCase) ? "link" : "login";

        if (safeMode == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["Error"] = "Please sign in first, then link your account.";
                return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
            }

            var linkResult = await _facade.LinkAsync(ticket, recaptchaToken ?? string.Empty, ct);

            if (linkResult.IsSuccess)
                TempData["Success"] = $"{provider} account linked successfully.";
            else
                TempData["Error"] = linkResult.Error ?? "Could not link provider.";

            if (linkResult.RequireSignOut)
                return RedirectToAction("SignIn", "Auth", new { area = "Auth" });

            return RedirectToAction("Index", "ExternalProviders", new { area = "Auth" });
        }

        var outcome = await _facade.LoginAsync(ticket, recaptchaToken ?? string.Empty, ct);

        if (outcome.IsSuccess)
            return RedirectLocal(safeReturn);

        if (outcome.IsNotLinked)
        {
            TempData["LoginError"] = outcome.Error;
            return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
        }

        TempData["LoginError"] = outcome.Error ?? "External sign-in failed.";
        return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
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
