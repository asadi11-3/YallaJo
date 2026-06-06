using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Content.Facades;
using YallaJo.Web.Areas.Content.Models.CreatorApplication;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Content.Controllers;

[Area("Content")]
[Authorize]
public sealed class CreatorApplicationController : BaseController
{
    private readonly CreatorApplicationFacade _facade;

    public CreatorApplicationController(CreatorApplicationFacade facade) => _facade = facade;

    [HttpGet("content/creators/apply")]
    [RequirePermission(WebPermission.Creator.Submit)]
    public async Task<IActionResult> Apply(CancellationToken ct)
    {
        var result = await _facade.GetApplyAsync(ct: ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Application));
        }

        return View(result.Data);
    }

    [HttpPost("content/creators/apply")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Submit)]
    public async Task<IActionResult> Apply(CreatorApplicationFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return await ReloadApplyAsync(form, ct);
        }

        var result = await _facade.ApplyAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error);
            }

            return await ReloadApplyAsync(form, ct);
        }

        SetSuccess("Your creator application was submitted for review.");
        return RedirectToAction(nameof(Application));
    }

    [HttpGet("content/creators/application")]
    [RequirePermission(WebPermission.Creator.Read)]
    public async Task<IActionResult> Application(CancellationToken ct)
    {
        var result = await _facade.GetApplicationAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Apply));
        }

        return View(result.Data);
    }

    [HttpPost("content/creators/application")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Update)]
    public async Task<IActionResult> Update(CreatorApplicationFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return await ReloadApplicationAsync(form, ct);
        }

        var result = await _facade.UpdateApplicationAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error);
            }

            return await ReloadApplicationAsync(form, ct);
        }

        SetSuccess("Your changes were saved.");
        return RedirectToAction(nameof(Application));
    }

    [HttpPost("content/creators/application/submit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Submit)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var result = await _facade.SubmitApplicationAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            SetError(result.Error);
        }
        else
        {
            SetSuccess("Your application was submitted for review.");
        }

        return RedirectToAction(nameof(Application));
    }

    [HttpGet("content/creators/redeem")]
    [RequirePermission(WebPermission.Creator.RedeemInvitation)]
    public IActionResult Redeem() => View(new RedeemCreatorInvitationVm());

    [HttpPost("content/creators/redeem")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.RedeemInvitation)]
    public async Task<IActionResult> Redeem(RedeemCreatorInvitationVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await _facade.RedeemAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error);
            }

            return View(vm);
        }

        SetSuccess("Invitation redeemed.");
        return RedirectToAction(nameof(Application));
    }

    private async Task<IActionResult> ReloadApplyAsync(CreatorApplicationFormVm form, CancellationToken ct)
    {
        var result = await _facade.GetApplyAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Application));
        }

        return View(nameof(Apply), result.Data);
    }

    private async Task<IActionResult> ReloadApplicationAsync(CreatorApplicationFormVm form, CancellationToken ct)
    {
        var result = await _facade.GetApplicationAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess && result.Data is { } vm)
        {
            vm.Form = form;
            return View(nameof(Application), vm);
        }

        SetError(result.Error);
        return RedirectToAction(nameof(Application));
    }
}
