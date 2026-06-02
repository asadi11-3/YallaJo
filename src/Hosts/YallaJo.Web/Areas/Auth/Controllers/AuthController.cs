using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Facades;
using YallaJo.Web.Areas.Auth.Models.ForgotPassword;
using YallaJo.Web.Areas.Auth.Models.Login;
using YallaJo.Web.Areas.Auth.Models.Register;
using YallaJo.Web.Areas.Auth.Models.ResetPassword;
using YallaJo.Web.Areas.Auth.Models.VerifyEmail;
using YallaJo.Web.Infrastructure.Authentication.Claims;
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

    public AuthController(
        LoginFacade login,
        RegisterFacade register,
        ForgotPasswordFacade forgot,
        ResetPasswordFacade reset,
        VerifyEmailFacade verify,
        LogoutFacade logout,
        LogoutAllFacade logoutAll)
    {
        _login = login;
        _register = register;
        _forgot = forgot;
        _reset = reset;
        _verify = verify;
        _logout = logout;
        _logoutAll = logoutAll;
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
            ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
        }

        return View(vm);
    }

    // ── Sign up ───────────────────────────────────────────────────────────────

    [HttpGet("sign-up")]
    public IActionResult SignUp()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Sessions", new { area = "Auth" });

        return View(new RegisterVm());
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
            SetSuccess("Registration successful. Please check your email for a verification code.");
            return RedirectToAction(nameof(TwoFactor), new { email = result.Email });
        }

        if (result.ValidationErrors is { Count: > 0 })
        {
            foreach (var (field, errors) in result.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
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
            SetSuccess("If that email is registered, a reset code has been sent.");
            return RedirectToAction(nameof(ResetPassword), new { email = vm.Email });
        }

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Request failed.");
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
            SetSuccess("Password reset successfully. Please sign in.");
            return RedirectToAction(nameof(SignIn));
        }

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Password reset failed.");
        return View(vm);
    }

    // ── Two-factor / email verification ─────────────────────────────────────────

    [HttpGet("two-factor-auth")]
    public IActionResult TwoFactor(string? email = null) =>
        View(new VerifyEmailVm { Email = email ?? string.Empty });

    [HttpPost("two-factor-auth")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactor(VerifyEmailVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _verify.HandleAsync(vm, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        // On success the facade has already signed the user in via IWebSignInService.
        if (result.IsSuccess)
            return RedirectToAction("Index", "Sessions", new { area = "Auth" });

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Email verification failed.");
        return View(vm);
    }

    [HttpPost("resend-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.Email))
            return BadRequest(new { error = "Email is required." });

        if (string.IsNullOrWhiteSpace(payload.RecaptchaToken))
            return BadRequest(new { error = "Verification failed. Please try again." });

        var outcome = await _verify.ResendOtpAsync(payload.Email, "EmailVerification", payload.RecaptchaToken, ct);

        if (outcome.IsSuccess)
            return Ok(new { message = "A new code has been sent." });

        var status = outcome.StatusCode is >= 400 and < 600 ? outcome.StatusCode : 500;
        return StatusCode(status, new { error = outcome.Error ?? "Could not resend code." });
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
