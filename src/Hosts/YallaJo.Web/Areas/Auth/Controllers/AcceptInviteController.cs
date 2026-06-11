using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Auth.Models.AcceptInvite;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Areas.Auth.Facades;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[AllowAnonymous]
[Route("auth/accept-invite")]
public sealed class AcceptInviteController : BaseController
{
    private readonly AcceptInviteFacade _facade;
    private readonly IStringLocalizer<YallaJo.Web.Resources.SharedResource> _localizer;

    public AcceptInviteController(
        AcceptInviteFacade facade,
        IStringLocalizer<YallaJo.Web.Resources.SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet("")]
    public IActionResult Index(string? email = null, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return View("Status", new InviteStatusVm { Kind = InviteStatusKind.InvalidLink });
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
            SetSuccess(_localizer["Auth.Flash.InviteAccepted"].Value);
            return RedirectToLogin();
        }

        if (result.IsExpired)
        {
            return View("Status", new InviteStatusVm { Kind = InviteStatusKind.Expired, Email = vm.Email });
        }

        if (result.IsAlreadyCompleted)
        {
            SetSuccess(result.Error ?? string.Empty);
            return RedirectToLogin();
        }

        // AcceptInviteResult is a bespoke outcome type (not ApiResult), so the
        // BaseController.ApplyValidationErrors overloads don't apply — the copy loop
        // stays manual here (same situation as AuthController's Login/Register results).
        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Auth.Flash.InviteFailed"].Value);
        return View(vm);
    }
}

