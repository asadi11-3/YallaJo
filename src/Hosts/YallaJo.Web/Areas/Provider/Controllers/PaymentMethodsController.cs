using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.PaymentMethods;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
[RequirePermission(WebPermission.ProviderPaymentMethod.Read)]
public sealed class PaymentMethodsController : BaseController
{
    private readonly ProviderPaymentMethodsFacade _paymentMethods;

    public PaymentMethodsController(ProviderPaymentMethodsFacade paymentMethods) => _paymentMethods = paymentMethods;

    [HttpGet("provider/payment-methods")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _paymentMethods.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new PaymentMethodsVm());
        }

        return View(result.Data);
    }

    [HttpPost("provider/payment-methods/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderPaymentMethod.Create)]
    public async Task<IActionResult> Create(CreatePaymentMethodFormVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
            return await ReloadAsync(form, ct);

        var result = await _paymentMethods.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return await ReloadAsync(form, ct);
        }

        SetSuccess("Payment method added.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("provider/payment-methods/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderPaymentMethod.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _paymentMethods.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        SetFlash(result, "Payment method deleted.", "Could not delete the payment method.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(CreatePaymentMethodFormVm form, CancellationToken ct)
    {
        var result = await _paymentMethods.GetAsync(ct);
        var vm = result is { IsSuccess: true, Data: { } data } ? data : new PaymentMethodsVm();
        vm.Form = form;
        return View(nameof(Index), vm);
    }

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "PaymentMethods";
        ViewBag.Sidebar = new ProviderSidebarVm { DisplayName = User.Identity?.Name ?? "Provider" };
    }
}
