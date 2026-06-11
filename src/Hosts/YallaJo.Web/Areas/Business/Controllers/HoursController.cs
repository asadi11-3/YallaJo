using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.Hours;
using YallaJo.Web.Areas.Business.Models.Settings;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class HoursController : BusinessControllerBase
{
    private const string SettingsView = "~/Areas/Business/Views/MyBusinesses/Settings.cshtml";

    private readonly BusinessHoursFacade _facade;

    public HoursController(BusinessHoursFacade facade) => _facade = facade;

    [HttpGet("business/businesses/{id:guid}/hours")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        SetSidebar("Hours", id);
        var result = await _facade.GetAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction("Index", "MyBusinesses");
        }
        return View(SettingsView, ToSettingsVm(result.Data));
    }

    [HttpPost("business/businesses/{id:guid}/hours/save")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BusinessHours.Update)]
    public async Task<IActionResult> Save(Guid id, HoursVm form, CancellationToken ct)
    {
        SetSidebar("Hours", id);

        if (!ModelState.IsValid)
            return await ReloadAsync(id, form, ct);

        var result = await _facade.SaveAsync(id, form.Days, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(id, form, ct);
        }

        SetSuccess(L["Business.Flash.HoursUpdated"].Value);
        return RedirectToAction(nameof(Index), new { id });
    }

    private async Task<IActionResult> ReloadAsync(Guid id, HoursVm form, CancellationToken ct)
    {
        var result = await _facade.GetAsync(id, ct);
        var vm = result is { IsSuccess: true, Data: not null }
            ? result.Data
            : new HoursVm { BusinessId = id, Days = form.Days };
        vm.Days = form.Days;
        return View(SettingsView, ToSettingsVm(vm));
    }

    private static BusinessSettingsVm ToSettingsVm(HoursVm hours) => new()
    {
        BusinessId = hours.BusinessId,
        BusinessName = hours.BusinessName,
        ActiveTab = BusinessSettingsVm.TabHours,
        Hours = hours,
    };
}
