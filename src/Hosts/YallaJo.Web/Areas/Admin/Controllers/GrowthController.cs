using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Growth;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

/// <summary>
/// §8.8 Growth tools — seasonality rules, holiday calendar, photogenic flags,
/// A/B experiments, and re-engagement segment queries. The page-level read is gated
/// by Batch.Read (the list endpoints' permission); each mutation carries its own
/// backend-mirrored permission.
/// </summary>
[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Batch.Read)]
public sealed class GrowthController : BaseController
{
    private readonly GrowthFacade _facade;
    private readonly LookupsFacade _lookups;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GrowthController(GrowthFacade facade, LookupsFacade lookups, IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
        _facade = facade;
        _lookups = lookups;
    }

    // ── GET /admin/growth ─────────────────────────────────────────────────────────
    [HttpGet("admin/growth")]
    public async Task<IActionResult> Index(int? year, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Growth";

        var result = await _facade.GetIndexAsync(year, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new GrowthVm { Year = year ?? DateTime.UtcNow.Year });
        }

        return View(result.Data);
    }

    // ── Seasonality ───────────────────────────────────────────────────────────────
    [HttpPost("admin/growth/seasonality")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SeasonalityRule.Create)]
    public async Task<IActionResult> CreateSeasonalityRule(CreateSeasonalityRuleVm form, [FromForm] string? placeQuery, CancellationToken ct)
    {
        // PE1/F10: JS fills the hidden PlaceId; without JS the typed query is resolved
        // server-side (GUID paste first, then a name lookup via the suggest proxy).
        if (form.PlaceId == Guid.Empty && !string.IsNullOrWhiteSpace(placeQuery))
        {
            form.PlaceId = Guid.TryParse(placeQuery.Trim(), out var parsed)
                ? parsed
                : await _lookups.ResolvePlaceIdAsync(placeQuery, ct) ?? Guid.Empty;
        }

        if (form.PlaceId == Guid.Empty)
        {
            SetError(_localizer["Admin.Growth.Flash.PlaceNotFound"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.CreateSeasonalityRuleAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Growth.Flash.SeasonalityCreated"].Value, _localizer["Admin.Growth.Flash.SeasonalityCreateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/growth/seasonality/{ruleId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SeasonalityRule.Delete)]
    public async Task<IActionResult> DeactivateSeasonalityRule(Guid ruleId, CancellationToken ct)
    {
        var result = await _facade.DeactivateSeasonalityRuleAsync(ruleId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Growth.Flash.SeasonalityDeactivated"].Value, _localizer["Admin.Growth.Flash.SeasonalityDeactivateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // ── Holiday calendar ────────────────────────────────────────────────────────────
    [HttpPost("admin/growth/holidays")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.HolidayCalendar.Create)]
    public async Task<IActionResult> CreateHoliday(CreateHolidayVm form, CancellationToken ct)
    {
        var result = await _facade.CreateHolidayAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Growth.Flash.HolidayCreated"].Value, _localizer["Admin.Growth.Flash.HolidayCreateFailed"].Value);
        return RedirectToAction(nameof(Index), new { year = form.Year });
    }

    // ── Photogenic ──────────────────────────────────────────────────────────────────
    [HttpPost("admin/growth/photogenic")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Photogenic.Update)]
    public async Task<IActionResult> SetPhotogenic(string? kind, Guid entityId, bool isPhotogenic, CancellationToken ct)
    {
        var result = await _facade.SetPhotogenicAsync(kind, entityId, isPhotogenic, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(
            result,
            isPhotogenic ? _localizer["Admin.Growth.Flash.PhotogenicMarked"].Value : _localizer["Admin.Growth.Flash.PhotogenicCleared"].Value,
            _localizer["Admin.Growth.Flash.PhotogenicUpdateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // ── A/B experiments ───────────────────────────────────────────────────────────
    [HttpPost("admin/growth/experiments")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Experiment.Create)]
    public async Task<IActionResult> CreateExperiment(CreateExperimentVm form, CancellationToken ct)
    {
        var result = await _facade.CreateExperimentAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Growth.Flash.ExperimentCreated"].Value, _localizer["Admin.Growth.Flash.ExperimentCreateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/growth/experiments/{experimentId:guid}/start")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Experiment.Update)]
    public async Task<IActionResult> StartExperiment(Guid experimentId, CancellationToken ct)
    {
        var result = await _facade.StartExperimentAsync(experimentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Growth.Flash.ExperimentStarted"].Value, _localizer["Admin.Growth.Flash.ExperimentStartFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/growth/experiments/{experimentId:guid}/complete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Experiment.Update)]
    public async Task<IActionResult> CompleteExperiment(Guid experimentId, CancellationToken ct)
    {
        var result = await _facade.CompleteExperimentAsync(experimentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Growth.Flash.ExperimentCompleted"].Value, _localizer["Admin.Growth.Flash.ExperimentCompleteFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // ── Re-engagement segments (read-only query) ─────────────────────────────────────
    [HttpGet("admin/growth/segments")]
    public async Task<IActionResult> Segment(string? rule, string? entityKind, Guid? entityId, CancellationToken ct)
    {
        var result = await _facade.GetSegmentAsync(rule, entityKind, entityId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        ViewData["SegmentRule"] = rule;
        ViewData["SegmentJson"] = result.Data;
        ViewData["AdminNav"] = "Growth";

        var index = await _facade.GetIndexAsync(null, ct);
        return View(nameof(Index), index.IsSuccess && index.Data is not null ? index.Data : new GrowthVm());
    }
}
