using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;
using YallaJo.Web.Infrastructure.Authentication.Claims;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Security.Recaptcha;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

/// <summary>
/// OAuth entry points for the BFF.
///
/// <para>Flow — login:</para>
/// <list type="number">
///   <item><description><c>POST /auth/external/challenge</c> → Challenge for provider.</description></item>
///   <item><description>Provider authenticates user, redirects back.</description></item>
///   <item><description><c>GET /auth/external/callback</c> → renders a short
///   interstitial HTML page that collects a reCAPTCHA v3 token client-side.</description></item>
///   <item><description><c>POST /auth/external/complete</c> → verified ticket
///   + reCAPTCHA token are forwarded to the API for login or link.</description></item>
/// </list>
///
/// <para>Security notes:</para>
/// <list type="bullet">
///   <item><description>Challenge is POST + anti-forgery to block CSRF-driven
///   linking attacks.</description></item>
///   <item><description><c>returnUrl</c> is always normalized to a local URL.</description></item>
///   <item><description>The linking flow records the owning user id inside
///   the challenge state; the callback refuses to link if that id no longer
///   matches the currently signed-in user.</description></item>
///   <item><description>The intermediate cookie is always signed out in the
///   callback — including on failure.</description></item>
///   <item><description>Complete is POST + anti-forgery. The reCAPTCHA token
///   on Complete is enforced by the API — not by this controller — so the
///   check cannot be bypassed by skipping the interstitial.</description></item>
/// </list>
/// </summary>
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

    // ExternalAuthController and ExternalProvidersController deliberately share
    // the "ExternalProviders" feature folder — they are two controllers for one
    // feature (OAuth round-trip + linked-providers management). The default
    // ~/Areas/{area}/Features/{controller}/Views/{view}.cshtml convention would
    // send Razor looking for Complete.cshtml under Features/ExternalAuth/Views/
    // which does not exist. Use the explicit feature-folder path so the view
    // resolves regardless of which controller renders it.
    private const string CompleteViewPath =
        "~/Areas/Auth/Features/ExternalProviders/Views/Complete.cshtml";

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

    /// <summary>
    /// Common callback. Reads the provider identity from the intermediate cookie,
    /// signs that cookie out, mints a short-lived BFF ticket, then hands off to
    /// an interstitial view that collects a reCAPTCHA token client-side and
    /// posts everything to <see cref="Complete"/>.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string provider, string? mode)
    {
        if (!IsProviderAvailable(provider))
            return BadRequest("Provider is not configured.");

        var authResult = await HttpContext.AuthenticateAsync(
            ExternalProviderConstants.ExternalSignInScheme);

        // Always sign out the intermediate cookie — even on failure.
        await HttpContext.SignOutAsync(ExternalProviderConstants.ExternalSignInScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            _logger.LogInformation("External auth callback for {Provider} failed (no principal).", provider);
            return RedirectToAction("Index", "Login", new { area = "Auth" });
        }

        var items = authResult.Properties?.Items ?? new Dictionary<string, string?>();
        var storedMode = items.TryGetValue(ModeItemKey, out var m) ? m : mode;
        var storedReturn = NormalizeReturnUrl(items.TryGetValue(ReturnUrlItemKey, out var r) ? r : null);

        var providerUserId = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);

        // Use the shared constant so the wire-format claim name is defined
        // in ONE place (Web BFF issues it, API verifier reads it). A previous
        // version hardcoded the literal string here which made it drift-prone.
        var emailVerifiedClaim = authResult.Principal.FindFirstValue(
            ExternalAuthTicketClaims.EmailVerified);
        var emailVerified = bool.TryParse(emailVerifiedClaim, out var ev) && ev;

        // Provider-surfaced names — used by the API's auto-create path on
        // first-time external sign-in. Both Google ("given_name"/"family_name")
        // and Facebook ("first_name"/"last_name") are already mapped by the
        // respective default ClaimActions to ClaimTypes.GivenName/Surname.
        var firstName = authResult.Principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = authResult.Principal.FindFirstValue(ClaimTypes.Surname);

        // Diagnostic log: without this, a runtime failure inside the auto-link
        // path on the API looks like a total black box from the Web side.
        // Logs at Information because it fires on EVERY external-login round
        // trip — operators can toggle it via category filter.
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
            return RedirectToAction("Index", "Login", new { area = "Auth" });
        }

        var safeMode = string.Equals(storedMode, "link", StringComparison.OrdinalIgnoreCase) ? "link" : "login";

        if (safeMode == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["LinkMessage"] = "Please sign in first, then link your account.";
                return RedirectToAction("Index", "Login", new { area = "Auth" });
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

        // Mint the single-use BFF ticket now — its own TTL (2 min) bounds how
        // long the interstitial can stall before the ticket expires.
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

    /// <summary>
    /// Interstitial POST-back. Receives the BFF-signed ticket plus a reCAPTCHA
    /// v3 token minted in the browser, and hands both off to the API. Anti-forgery
    /// is enforced — the interstitial page includes the token.
    /// </summary>
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
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        var safeReturn = NormalizeReturnUrl(returnUrl);
        var safeMode = string.Equals(mode, "link", StringComparison.OrdinalIgnoreCase) ? "link" : "login";

        if (safeMode == "link")
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["LinkMessage"] = "Please sign in first, then link your account.";
                return RedirectToAction("Index", "Login", new { area = "Auth" });
            }

            var linkResult = await _facade.LinkAsync(ticket, recaptchaToken ?? string.Empty, ct);

            TempData["ProviderMessage"] = linkResult.IsSuccess
                ? $"{provider} account linked successfully."
                : linkResult.Error ?? "Could not link provider.";

            if (linkResult.RequireSignOut)
                return RedirectToAction("Index", "Login", new { area = "Auth" });

            return RedirectToAction("Index", "ExternalProviders", new { area = "Auth" });
        }

        var outcome = await _facade.LoginAsync(ticket, recaptchaToken ?? string.Empty, ct);

        if (outcome.IsSuccess)
            return RedirectLocal(safeReturn);

        if (outcome.IsNotLinked)
        {
            TempData["LoginError"] = outcome.Error;
            return RedirectToAction("Index", "Login", new { area = "Auth" });
        }

        TempData["LoginError"] = outcome.Error ?? "External sign-in failed.";
        return RedirectToAction("Index", "Login", new { area = "Auth" });
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
