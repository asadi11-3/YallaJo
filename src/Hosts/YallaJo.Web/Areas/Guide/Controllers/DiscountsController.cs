using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Discounts;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class DiscountsController : GuideBaseController
{
    private readonly GuideDiscountsFacade _discounts;

    public DiscountsController(GuideDiscountsFacade discounts) => _discounts = discounts;

    [HttpGet("guide/discounts")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Discounts");
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
        SetNav("Discounts");

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

    [HttpPost("guide/discounts/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditDiscountFormVm form, CancellationToken ct = default)
    {
        SetNav("Discounts");

        if (form.ValidUntil.HasValue && form.ValidUntil.Value < form.ValidFrom)
        {
            ModelState.AddModelError(nameof(form.ValidUntil), "Valid-until must be on or after the valid-from date.");
        }

        // Re-render with the user's submitted values preserved instead of flash+redirect (no data loss).
        if (!ModelState.IsValid)
        {
            return await ReloadEditAsync(id, form, ct);
        }

        var result = await _discounts.UpdateAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error ?? "Could not update the discount.");
            }

            return await ReloadEditAsync(id, form, ct);
        }

        SetSuccess("Discount updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/discounts/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
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
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var vm = result.IsSuccess && result.Data is not null ? result.Data : new DiscountsVm();
        vm.Form = form;
        return View(nameof(Index), vm);
    }

    private async Task<IActionResult> ReloadEditAsync(Guid id, EditDiscountFormVm form, CancellationToken ct)
    {
        var result = await _discounts.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var vm = result.IsSuccess && result.Data is not null ? result.Data : new DiscountsVm();
        vm.EditForm = form;
        vm.OpenEditId = id;
        return View(nameof(Index), vm);
    }
}
