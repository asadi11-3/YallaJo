using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail.Requests;
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

    [HttpPost("auth/verifyemail/resendotp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.Email))
            return BadRequest(new { error = "Email is required." });

        var outcome = await _facade.ResendOtpAsync(payload.Email, "EmailVerification", ct);
        if (outcome.IsSuccess)
            return Ok(new { message = "A new code has been sent." });

        // Preserve the real status from the backend (429 throttle vs 500 SMTP
        // failure vs 400 validation). Collapsing everything to 429 masked
        // server errors from both users and ops.
        var status = outcome.StatusCode is >= 400 and < 600
            ? outcome.StatusCode
            : StatusCodes.Status500InternalServerError;
        return StatusCode(status, new { error = outcome.Error ?? "Could not resend code." });
    }
}
