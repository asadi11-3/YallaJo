using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Discounts;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class DiscountsController : BaseController
{
    private readonly GuideDiscountsFacade _discounts;

    public DiscountsController(GuideDiscountsFacade discounts) => _discounts = discounts;

    [HttpGet("guide/discounts")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _discounts.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new DiscountsVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/discounts/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDiscountFormVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (form.ValidUntil.HasValue && form.ValidUntil.Value < form.ValidFrom)
        {
            ModelState.AddModelError(nameof(form.ValidUntil), "Valid-until must be on or after the valid-from date.");
        }

        if (!ModelState.IsValid)
        {
            return await ReloadAsync(form, ct);
        }

        var result = await _discounts.CreateAsync(form, ct);
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

            return await ReloadAsync(form, ct);
        }

        SetSuccess("Discount created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/discounts/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _discounts.DeactivateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Discount deactivated.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(CreateDiscountFormVm form, CancellationToken ct)
    {
        var result = await _discounts.GetAsync(ct);
        var vm = result.IsSuccess && result.Data is not null ? result.Data : new DiscountsVm();
        vm.Form = form;
        return View(nameof(Index), vm);
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "Discounts";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
