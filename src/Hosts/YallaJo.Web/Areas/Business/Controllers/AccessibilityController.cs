using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.Accessibility;
using YallaJo.Web.Areas.Business.Models.Settings;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class AccessibilityController : BusinessControllerBase
{
    private const string SettingsView = "~/Areas/Business/Views/MyBusinesses/Settings.cshtml";

    private readonly BusinessAccessibilityFacade _facade;

    public AccessibilityController(BusinessAccessibilityFacade facade) => _facade = facade;

    [HttpGet("business/businesses/{id:guid}/accessibility")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct = default)
    {
        SetSidebar("Accessibility", id);
        var result = await _facade.GetAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction("Index", "MyBusinesses");
        }

        return View(SettingsView, ToSettingsVm(result.Data));
    }

    [HttpPost("business/businesses/{id:guid}/accessibility/save")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AccessibilityFeature.Update)]
    public async Task<IActionResult> Save(Guid id, AccessibilityVm form, CancellationToken ct = default)
    {
        SetSidebar("Accessibility", id);

        if (!ModelState.IsValid)
        {
            return await ReloadAsync(id, form, ct);
        }

        var result = await _facade.SaveAsync(id, form.Features, ct);
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

            return await ReloadAsync(id, form, ct);
        }

        SetSuccess("Accessibility features updated.");
        return RedirectToAction(nameof(Index), new { id });
    }

    private async Task<IActionResult> ReloadAsync(Guid id, AccessibilityVm form, CancellationToken ct)
    {
        var result = await _facade.GetAsync(id, ct);
        var vm = result is { IsSuccess: true, Data: not null } ? result.Data : new AccessibilityVm { BusinessId = id };
        vm.Features = form.Features;
        return View(SettingsView, ToSettingsVm(vm));
    }

    private static BusinessSettingsVm ToSettingsVm(AccessibilityVm accessibility) => new()
    {
        BusinessId = accessibility.BusinessId,
        BusinessName = accessibility.BusinessName,
        ActiveTab = BusinessSettingsVm.TabAccessibility,
        Accessibility = accessibility,
    };
}
