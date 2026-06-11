// <copyright file="SeoWeatherController.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.SeoWeather;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Weather.Read)]
public sealed class SeoWeatherController : BaseController
{
    private readonly SeoWeatherFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SeoWeatherController(SeoWeatherFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] WeatherLookupRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "SeoWeather";
        var result = await _facade.GetAsync(request.PlaceId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new SeoWeatherVm { PlaceId = request.PlaceId });
        }

        return View(result.Data);
    }

    [HttpPost("admin/seo/weather/refresh")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Weather.Refresh)]
    public async Task<IActionResult> Refresh(RefreshWeatherFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError(_localizer["Admin.SeoWeather.Flash.FieldsRequired"].Value);
            return RedirectToAction(nameof(Index), new { placeId = form.PlaceId });
        }

        var result = await _facade.RefreshAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.SeoWeather.Flash.Refreshed"].Value, _localizer["Admin.SeoWeather.Flash.RefreshFailed"].Value);
        return RedirectToAction(nameof(Index), new { placeId = form.PlaceId });
    }

    [HttpPost("admin/seo/weather/purge")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Weather.Delete)]
    public async Task<IActionResult> Purge([FromForm] Guid placeId, CancellationToken ct)
    {
        var result = await _facade.PurgeAsync(placeId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.SeoWeather.Flash.CachePurged"].Value, _localizer["Admin.SeoWeather.Flash.CachePurgeFailed"].Value);
        return RedirectToAction(nameof(Index), new { placeId });
    }

    [HttpPost("admin/seo/weather/reset-budget")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Weather.Update)]
    public async Task<IActionResult> ResetBudget(ResetBudgetFormVm form, CancellationToken ct)
    {
        var result = await _facade.ResetBudgetAsync(form.Date, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.SeoWeather.Flash.BudgetReset"].Value, _localizer["Admin.SeoWeather.Flash.BudgetResetFailed"].Value);
        return RedirectToAction(nameof(Index));
    }
}
