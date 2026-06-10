using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Auth.Facades;
using YallaJo.Web.Areas.Auth.Models.ForgotPassword;
using YallaJo.Web.Areas.Auth.Models.Login;
using YallaJo.Web.Areas.Auth.Models.Register;
using YallaJo.Web.Areas.Auth.Models.ResetPassword;
using YallaJo.Web.Areas.Auth.Models.VerifyEmail;
using YallaJo.Web.Infrastructure.Authentication.Claims;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Auth.Controllers;
/// <summary>
/// Unified controller for the anonymous authentication pages: sign-in, sign-up,
/// forgot/reset-password and the two-factor (email OTP) verification flow.
///
/// Follows the "composite view / multiple facades" pattern (guide §9.5 Pattern A):
/// it composes the existing per-feature facades and never touches IApiClient directly.
/// External-provider sign-in stays on ExternalAuthController (an OAuth redirect flow,
/// not a page), so the provider buttons on the sign-in/sign-up views POST there.
/// </summary>
[Area("Auth")]
[AllowAnonymous]
[Route("auth")]
public sealed class AuthController : BaseController
{
    private readonly LoginFacade _login;
    private readonly RegisterFacade _register;
    private readonly ForgotPasswordFacade _forgot;
    private readonly ResetPasswordFacade _reset;
    private readonly VerifyEmailFacade _verify;
    private readonly LogoutFacade _logout;
    private readonly LogoutAllFacade _logoutAll;
    private readonly IPendingVerificationStore _pendingVerification;
    private readonly IStringLocalizer<YallaJo.Web.Resources.SharedResource> _localizer;

    public AuthController(
        LoginFacade login,
        RegisterFacade register,
        ForgotPasswordFacade forgot,
        ResetPasswordFacade reset,
        VerifyEmailFacade verify,
        LogoutFacade logout,
        LogoutAllFacade logoutAll,
        IPendingVerificationStore pendingVerification,
        IStringLocalizer<YallaJo.Web.Resources.SharedResource> localizer)
    {
        _login = login;
        _register = register;
        _forgot = forgot;
        _reset = reset;
        _verify = verify;
        _logout = logout;
        _logoutAll = logoutAll;
        _pendingVerification = pendingVerification;
        _localizer = localizer;
    }

    /// <summary>Display-mask for the pending e-mail, e.g. "j***@example.com".</summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return at == 1 ? $"{email[0]}***{email[at..]}" : email;
        return $"{email[0]}***{email[(at - 1)..]}";
    }

    // ── Sign in ───────────────────────────────────────────────────────────────

    [HttpGet("sign-in")]
    public IActionResult SignIn(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost("sign-in")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignIn(LoginVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _login.HandleAsync(vm, ct);

        if (result.IsSuccess)
            return RedirectToLocal(vm.ReturnUrl);

        if (result.ValidationErrors is { Count: > 0 })
        {
            foreach (var (field, errors) in result.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Auth.Flash.LoginFailed"].Value);
        }

        return View(vm);
    }

    // ── Sign up ───────────────────────────────────────────────────────────────

    [HttpGet("sign-up")]
    public IActionResult SignUp(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        // returnUrl is carried only into the social-login challenge forms (validated by
        // ExternalAuthController); the email registration path always goes to OTP first.
        return View(new RegisterVm { ReturnUrl = returnUrl });
    }

    [HttpPost("sign-up")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(RegisterVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _register.HandleAsync(vm, ct);

        if (result.IsSuccess)
        {
            // The pending e-mail travels in an encrypted, time-limited cookie — never in
            // the URL or a hidden field (PII + arbitrary-target OTP guessing).
            _pendingVerification.Issue(result.Email!);
            SetSuccess(_localizer["Auth.Flash.RegistrationSuccess"].Value);
            return RedirectToAction(nameof(TwoFactor));
        }

        if (result.ValidationErrors is { Count: > 0 })
        {
            foreach (var (field, errors) in result.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Auth.Flash.RegistrationFailed"].Value);
        }

        return View(vm);
    }

    // ── Forgot password ─────────────────────────────────────────────────────────

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVm());

    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _forgot.HandleAsync(vm, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Auth.Flash.ForgotUniform"].Value);
            return RedirectToAction(nameof(ResetPassword), new { email = vm.Email });
        }

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Auth.Flash.RequestFailed"].Value);
        return View(vm);
    }

    // ── Reset password ──────────────────────────────────────────────────────────

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string? email = null) =>
        View(new ResetPasswordVm { Email = email ?? string.Empty });

    [HttpPost("reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _reset.HandleAsync(vm, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Auth.Flash.ResetSuccess"].Value);
            return RedirectToAction(nameof(SignIn));
        }

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Auth.Flash.ResetFailed"].Value);
        return View(vm);
    }

    // ── Two-factor / email verification ─────────────────────────────────────────
    // The pending identity comes EXCLUSIVELY from the encrypted pending-verification
    // cookie issued by SignUp (hard cutover — the old ?email= parameter is no longer
    // honored). Without a valid cookie the OTP screen is unreachable, so the endpoint
    // cannot be pointed at an arbitrary account.

    [HttpGet("two-factor-auth")]
    public IActionResult TwoFactor()
    {
        var pendingEmail = _pendingVerification.TryRead();
        if (string.IsNullOrWhiteSpace(pendingEmail))
        {
            SetError(_localizer["Auth.Flash.VerificationExpired"].Value);
            return RedirectToLogin();
        }

        return View(new VerifyEmailVm
        {
            Email = pendingEmail,
            MaskedEmail = MaskEmail(pendingEmail),
        });
    }

    [HttpPost("two-factor-auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactor(VerifyEmailVm vm, CancellationToken ct)
    {
        // [BindNever] keeps any posted Email out of the model — re-resolve it from the
        // cookie so the OTP check is always bound to the registration that issued it.
        var pendingEmail = _pendingVerification.TryRead();
        if (string.IsNullOrWhiteSpace(pendingEmail))
        {
            SetError(_localizer["Auth.Flash.VerificationExpired"].Value);
            return RedirectToLogin();
        }

        vm.Email = pendingEmail;
        vm.MaskedEmail = MaskEmail(pendingEmail);

        if (!ModelState.IsValid)
            return View(vm);

        var result = await _verify.HandleAsync(vm, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        // On success the facade has already signed the user in via IWebSignInService.
        if (result.IsSuccess)
        {
            _pendingVerification.Clear();
            return RedirectToAction("Index", "Sessions", new { area = "Auth" });
        }

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Auth.Flash.VerifyFailed"].Value);
        return View(vm);
    }

    // Anti-enumeration (guide §security): the response is UNIFORM regardless of whether
    // the email maps to an account or what the upstream API said — no raw status codes,
    // no provider error text. A 404 vs 429 vs 400 distinction (or relayed API messages)
    // would let an attacker confirm account existence or probe rate-limit boundaries.
    // The only non-200 responses are input-shape guards that reveal nothing about
    // accounts. Same pattern as ForgotPassword's "If that email is registered…".
    //
    // The target e-mail is resolved from the encrypted pending-verification cookie —
    // the client no longer chooses who receives an OTP, killing the "spam OTPs to an
    // arbitrary address" abuse vector. The payload's Email field is ignored.
    [HttpPost("resend-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.RecaptchaToken))
            return BadRequest(new { error = _localizer["Auth.Resend.VerificationFailed"].Value });

        var pendingEmail = _pendingVerification.TryRead();
        if (string.IsNullOrWhiteSpace(pendingEmail))
        {
            // No pending verification in this browser. Stay uniform: report the same
            // neutral message rather than confirming/denying anything.
            return Ok(new { message = _localizer["Auth.Resend.Uniform"].Value });
        }

        // Outcome deliberately ignored beyond awaiting completion: success, unknown
        // email, and upstream errors all collapse into the same neutral message.
        await _verify.ResendOtpAsync(pendingEmail, "EmailVerification", payload.RecaptchaToken, ct);

        return Ok(new { message = _localizer["Auth.Resend.Uniform"].Value });
    }

    // ── Sign out ────────────────────────────────────────────────────────────────

    // The controller is [AllowAnonymous] for the public auth pages, so an explicit
    // [Authorize] here would be a no-op (ASP0026). Sign-out is intentionally safe to
    // call regardless of auth state: the facade clears whatever local cookie exists
    // and revokes the refresh token only when one is present.
    [HttpPost("sign-out")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Read the refresh-token claim at the HTTP boundary; the facade never touches HttpContext.
        var refreshToken = User.FindFirstValue(AppClaimTypes.RefreshToken);

        var result = await _logout.HandleAsync(refreshToken, ct);

        // The local cookie is always cleared by the facade, so we sign the user out regardless.
        // Surface a server-side revoke failure as a flash without blocking the sign-out.
        if (!result.IsSuccess)
            SetError(result.Error);

        return RedirectToAction(nameof(SignIn));
    }

    // Revokes every active session on all devices. Like Logout, this is safe to call
    // in any auth state — the facade always clears the local cookie.
    [HttpPost("sign-out-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var result = await _logoutAll.HandleAsync(ct);

        if (!result.IsSuccess)
            SetError(result.Error);

        return RedirectToAction(nameof(SignIn));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Sessions", new { area = "Auth" });
    }
}
