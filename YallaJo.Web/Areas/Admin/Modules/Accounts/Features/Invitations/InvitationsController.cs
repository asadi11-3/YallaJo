using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations.ViewModels;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.User.Create)]
public sealed class InvitationsController : Controller
{
    private readonly InvitationsFacade _facade;

    public InvitationsController(InvitationsFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index() =>
        View(new InviteUserVm());

    [HttpGet]
    public IActionResult Resend() =>
        View(new ResendInviteVm());

    [HttpPost("admin/invitations/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.Create)]
    public async Task<IActionResult> Create(InviteUserVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), vm);

        var result = await _facade.InviteAsync(vm, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (result.IsSuccess)
        {
            TempData["Success"] =
                $"Invite sent to {vm.Email}. They will receive an email to set a password and activate the account.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(nameof(Index), vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not send invite.");
        return View(nameof(Index), vm);
    }

    [HttpPost("admin/invitations/resend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.Create)]
    public async Task<IActionResult> ResendSubmit(ResendInviteVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(nameof(Resend), vm);

        var result = await _facade.ResendAsync(vm, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (result.IsSuccess)
        {
            TempData["Success"] =
                "If an invited account exists for that email, a new invite has been sent.";
            return RedirectToAction(nameof(Resend));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(nameof(Resend), vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not resend invite.");
        return View(nameof(Resend), vm);
    }
}
