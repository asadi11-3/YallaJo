using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.VerifyEmail;

[Area("Auth")]
[AllowAnonymous]
public sealed class VerifyEmailController : Controller
{
    private readonly VerifyEmailFacade _facade;

    public VerifyEmailController(VerifyEmailFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index(string? email = null) =>
        View(new VerifyEmailVm { Email = email ?? string.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(VerifyEmailVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.HandleAsync(vm, ct);

        if (result.IsSuccess)
            return RedirectToAction("Index", "Sessions", new { area = "Auth" });

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, msgs) in result.ValidationErrors)
                foreach (var m in msgs)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        return View(vm);
    }

    // POST /auth/verifyemail/resendotp  (AJAX)
    [HttpPost("auth/verifyemail/resendotp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.Email))
            return BadRequest(new { error = "Email is required." });

        var error = await _facade.ResendOtpAsync(payload.Email, "EmailVerification", ct);
        return error is null
            ? Ok(new { message = "A new code has been sent." })
            : StatusCode(429, new { error });
    }
}

public sealed record ResendOtpPayload(string Email);
