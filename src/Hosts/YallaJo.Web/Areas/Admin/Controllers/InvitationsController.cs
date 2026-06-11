using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Invitations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.User.Create)]
public sealed class InvitationsController : BaseController
{
    private readonly InvitationsFacade _facade;

    public InvitationsController(InvitationsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new InviteUserVm();
        var loaded = await PopulateRoleOptionsAsync(vm, ct);
        if (GuardSignOut(loaded) is { } signOut) return signOut;

        if (!loaded.IsSuccess)
            SetError(loaded.Error ?? "Could not load role options.");

        return View(vm);
    }

    // PE1: the standalone Resend view was retired; the resend form lives on Index.
    // Old deep links permanently redirect so bookmarks keep working.
    [HttpGet]
    public IActionResult Resend() =>
        RedirectToActionPermanent(nameof(Index));

    [HttpPost("admin/invitations/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.Create)]
    public async Task<IActionResult> Create(InviteUserVm vm, CancellationToken ct)
    {
        var loaded = await PopulateRoleOptionsAsync(vm, ct);
        if (GuardSignOut(loaded) is { } signOut) return signOut;

        if (!loaded.IsSuccess)
            ModelState.AddModelError(string.Empty, loaded.Error ?? "Could not load role options.");

        if (!ModelState.IsValid)
            return View(nameof(Index), vm);

        var result = await _facade.InviteAsync(vm, ct);

        if (GuardSignOut(result) is { } resultSignOut) return resultSignOut;

        if (result.IsSuccess)
        {
            SetSuccess(
                $"Invite sent to {vm.Email}. They will receive an email to set a password and activate the account.");
            return RedirectToAction(nameof(Index));
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not send invite.");

        return View(nameof(Index), vm);
    }

    [HttpPost("admin/invitations/resend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.Create)]
    public async Task<IActionResult> ResendSubmit([Bind(Prefix = "Resend")] ResendInviteVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return await IndexWithResend(vm, ct);

        var result = await _facade.ResendAsync(vm, ct);

        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("If an invited account exists for that email, a new invite has been sent.");
            return RedirectToAction(nameof(Index));
        }

        // UI-UX-F6: surface API validation errors against the prefixed resend fields.
        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Resend.{field}", m);
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not resend invite.");
        }

        return await IndexWithResend(vm, ct);
    }

    // PE1: resend failures re-render the Index page (which hosts the resend card).
    private async Task<IActionResult> IndexWithResend(ResendInviteVm resend, CancellationToken ct)
    {
        var vm = new InviteUserVm { Resend = resend };
        var loaded = await PopulateRoleOptionsAsync(vm, ct);

        if (!loaded.IsSuccess)
            SetError(loaded.Error ?? "Could not load role options.");

        return View(nameof(Index), vm);
    }

    private async Task<ApiResult> PopulateRoleOptionsAsync(InviteUserVm vm, CancellationToken ct)
    {
        var result = await _facade.GetInvitableRolesAsync(ct);
        if (!result.IsSuccess || result.Data is null)
        {
            vm.AvailableRoles = [];
            return result.IsUnauthorized
                ? ApiResult.ForceSignOut()
                : ApiResult.Fail(result.Error ?? "Could not load role options.");
        }

        vm.AvailableRoles = result.Data;
        return ApiResult.Ok();
    }
}
