using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Agency;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class AgencyController : GuideBaseController
{
    private readonly GuideAgencyFacade _facade;

    public AgencyController(GuideAgencyFacade facade) => _facade = facade;

    [HttpGet("guide/agency")]
    [RequirePermission(WebPermission.GuideAgency.Read)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Agency");
        var result = await _facade.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new AgencyVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/agency/apply")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideAgency.Create)]
    // Bind prefix matches the view's asp-for="ApplyForm.*" field names (the bare
    // parameter name "form" wouldn't match the posted "ApplyForm." prefix).
    public async Task<IActionResult> Apply([Bind(Prefix = "ApplyForm")] ApplyToAgencyFormVm form, CancellationToken ct = default)
    {
        SetNav("Agency");
        if (!ModelState.IsValid) return await ReloadAsync(form, ct);

        var result = await _facade.ApplyAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(form, ct);
        }

        SetSuccess(L["Guide.Flash.AgencyApplicationSubmitted"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/agency/invitations/{id:guid}/accept")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideAgency.Update)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.AcceptAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, L["Guide.Flash.InvitationAccepted"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/agency/invitations/{id:guid}/decline")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideAgency.Update)]
    public async Task<IActionResult> Decline(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.DeclineAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, L["Guide.Flash.InvitationDeclined"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/agency/leave")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideAgency.Delete)]
    public async Task<IActionResult> Leave(CancellationToken ct = default)
    {
        var result = await _facade.LeaveAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, L["Guide.Flash.LeftAgency"]);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(ApplyToAgencyFormVm form, CancellationToken ct)
    {
        var result = await _facade.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var vm = result.IsSuccess && result.Data is not null ? result.Data : new AgencyVm();
        vm.ApplyForm = form;
        return View(nameof(Index), vm);
    }
}
