using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.PaymentMethods;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// Provider payout payment methods. The standalone page was retired into the Finance
/// hub (Methods tab); <see cref="Index"/> remains only as a permanent redirect so old
/// links keep working. All write actions PRG back to the Finance hub.
/// </summary>
[Area("Provider")]
[Authorize]
[RequirePermission(WebPermission.ProviderPaymentMethod.Read)]
public sealed class PaymentMethodsController : BaseController
{
    private readonly ProviderPaymentMethodsFacade _paymentMethods;

    public PaymentMethodsController(ProviderPaymentMethodsFacade paymentMethods) => _paymentMethods = paymentMethods;

    /// <summary>Retired page — 301 into the Finance hub's Methods tab.</summary>
    [HttpGet("provider/payment-methods")]
    public IActionResult Index() => RedirectPermanent("/provider/finance#methods");

    [HttpPost("provider/payment-methods/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderPaymentMethod.Create)]
    public async Task<IActionResult> Create(CreatePaymentMethodFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BackToFinanceWithModelErrors();

        var result = await _paymentMethods.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error ?? "Could not add the payment method.");
            return RedirectToFinanceMethods();
        }

        SetSuccess("Payment method added.");
        return RedirectToFinanceMethods();
    }

    [HttpPost("provider/payment-methods/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderPaymentMethod.Update)]
    public async Task<IActionResult> Edit(Guid id, CreatePaymentMethodFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BackToFinanceWithModelErrors();

        var result = await _paymentMethods.UpdateAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error ?? "Could not update the payment method.");
            return RedirectToFinanceMethods();
        }

        SetSuccess("Payment method updated.");
        return RedirectToFinanceMethods();
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
        return RedirectToFinanceMethods();
    }

    /// <summary>
    /// PRG fallback for invalid form posts: flash the first model error and bounce back
    /// to the Finance hub's Methods tab (the standalone view no longer exists).
    /// </summary>
    private IActionResult BackToFinanceWithModelErrors()
    {
        var firstError = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

        SetError(firstError ?? "Please correct the highlighted fields and try again.");
        return RedirectToFinanceMethods();
    }

    private IActionResult RedirectToFinanceMethods() => Redirect("/provider/finance#methods");
}
