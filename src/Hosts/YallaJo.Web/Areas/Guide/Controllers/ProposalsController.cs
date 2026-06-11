using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Proposals;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class ProposalsController : GuideBaseController
{
    private readonly GuideProposalsFacade _proposals;

    public ProposalsController(GuideProposalsFacade proposals) => _proposals = proposals;

    [HttpGet("guide/proposals")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Proposals");
        var result = await _proposals.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ProposalsVm());
        }

        return View(result.Data);
    }

    /// <summary>
    /// [Backend] B2 Web proxy — JSON options for the async place picker (F10/JS5).
    /// The browser never calls the API host directly; no-JS users keep the SSR select (PE1/PE2).
    /// </summary>
    [HttpGet("guide/proposals/places")]
    public async Task<IActionResult> PlaceLookup(string? term, CancellationToken ct = default)
    {
        var result = await _proposals.GetPlaceOptionsAsync(term, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        var items = result.Data ?? [];
        return Json(items.Select(p => new { id = p.Id, label = p.Label }));
    }

    [HttpPost("guide/proposals/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProposalFormVm form, CancellationToken ct = default)
    {
        SetNav("Proposals");
        if (!ModelState.IsValid)
        {
            return await ReloadAsync(form, ct);
        }

        var result = await _proposals.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error);
            }

            return await ReloadAsync(form, ct);
        }

        SetSuccess("Proposal created. You can review and submit it for approval.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/proposals/{id:guid}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct = default)
    {
        var result = await _proposals.SubmitAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Proposal submitted for review.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(CreateProposalFormVm form, CancellationToken ct)
    {
        var result = await _proposals.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var vm = result.IsSuccess && result.Data is not null ? result.Data : new ProposalsVm();
        vm.Form = form;
        return View(nameof(Index), vm);
    }
}
