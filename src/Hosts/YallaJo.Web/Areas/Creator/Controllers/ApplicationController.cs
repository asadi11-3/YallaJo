using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Application;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Creator.Controllers;

[Area("Creator")]
[Authorize]
public sealed class ApplicationController : BaseController
{
    private readonly CreatorApplicationFacade _facade;

    public ApplicationController(CreatorApplicationFacade facade) => _facade = facade;

    // GET /creator/application
    [HttpGet("creator/application")]
    [RequirePermission(WebPermission.Creator.Read)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _facade.GetApplicationFormAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new CreatorApplicationFormVm());
        }

        return View(result.Data);
    }

    // POST /creator/application/create  (first-time create → Draft) — Creator.Submit
    [HttpPost("creator/application/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Submit)]
    public async Task<IActionResult> Create(CreatorApplicationFormVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
        {
            await _facade.PopulateNicheOptionsAsync(form, ct);
            return View(nameof(Index), form);
        }

        var result = await _facade.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            // Validation errors bind to fields; other failures (e.g. 409 active-exists)
            // render inline in the form's validation summary.
            if (!ApplyValidationErrors(result))
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not create your creator application.");
            await _facade.PopulateNicheOptionsAsync(form, ct);
            return View(nameof(Index), form);
        }

        SetSuccess("Your creator application was created as a draft. Review it and submit when ready.");
        return RedirectToAction(nameof(Index));
    }

    // POST /creator/application/update  (edit Draft/MoreInfoNeeded) — Creator.Update
    [HttpPost("creator/application/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Update)]
    public async Task<IActionResult> Update(CreatorApplicationFormVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (form.ApplicationId is not { } applicationId)
        {
            SetError("There is no existing application to update.");
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            await _facade.PopulateNicheOptionsAsync(form, ct);
            return View(nameof(Index), form);
        }

        var result = await _facade.UpdateAsync(applicationId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not update your creator application.");
            await _facade.PopulateNicheOptionsAsync(form, ct);
            return View(nameof(Index), form);
        }

        SetSuccess("Your creator application was updated.");
        return RedirectToAction(nameof(Index));
    }

    // POST /creator/application/submit  (Draft → Pending) — Creator.Submit
    [HttpPost("creator/application/submit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Submit)]
    public async Task<IActionResult> Submit(Guid applicationId, CancellationToken ct = default)
    {
        var result = await _facade.SubmitAsync(applicationId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Your creator application was submitted for review.",
            "Could not submit your creator application.");
        return RedirectToAction(nameof(Index));
    }

    // GET /creator/application/invite  — Creator.RedeemInvitation
    [HttpGet("creator/application/invite")]
    [RequirePermission(WebPermission.Creator.RedeemInvitation)]
    public IActionResult Invite()
    {
        SetSidebar();
        return View(new RedeemInvitationVm());
    }

    // POST /creator/application/invite  (redeem token) — Creator.RedeemInvitation
    [HttpPost("creator/application/invite")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.RedeemInvitation)]
    public async Task<IActionResult> Invite(RedeemInvitationVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
            return View(form);

        var result = await _facade.RedeemAsync(form.Token, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return View(form);
        }

        SetSuccess("Invitation redeemed. Your creator application has been started.");
        return RedirectToAction(nameof(Index));
    }

    private void SetSidebar()
    {
        ViewData["CreatorNav"] = "Application";
        ViewBag.Sidebar = new CreatorSidebarVm { DisplayName = User.Identity?.Name ?? "Creator" };
    }
}
