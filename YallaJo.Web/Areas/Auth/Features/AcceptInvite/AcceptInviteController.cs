using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.AcceptInvite.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.AcceptInvite;

[Area("Auth")]
[AllowAnonymous]
[Route("auth/accept-invite")]
public sealed class AcceptInviteController : Controller
{
    private readonly AcceptInviteFacade _facade;

    public AcceptInviteController(AcceptInviteFacade facade) => _facade = facade;

    [HttpGet("")]
    public IActionResult Index(string? email = null, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return View("InvalidLink");
        }

        return View(new AcceptInviteVm
        {
            Email = email,
            Token = token,
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AcceptInviteVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _facade.HandleAsync(vm, ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] =
                "Your account is active. Please sign in with your email and new password.";
            return RedirectToAction("Index", "Login", new { area = "Auth" });
        }

        if (result.IsExpired)
        {
            return View("Expired", new ResendFromExpiredVm { Email = vm.Email });
        }

        if (result.IsAlreadyCompleted)
        {
            TempData["SuccessMessage"] = result.Error;
            return RedirectToAction("Index", "Login", new { area = "Auth" });
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not accept invite.");
        return View(vm);
    }
}

public sealed class ResendFromExpiredVm
{
    public string Email { get; set; } = string.Empty;
}
