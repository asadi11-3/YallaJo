using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Earnings;
using YallaJo.Web.Areas.Provider.Models.Finance;
using YallaJo.Web.Areas.Provider.Models.Invoices;
using YallaJo.Web.Areas.Provider.Models.PaymentMethods;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// Unified provider finance surface (§4.8) — one page with Payouts / Invoices / Methods tabs.
/// Pattern A composite: injects the three existing finance facades (CONTROLLER_AUTHORING_GUIDE §9.5);
/// the standalone Earnings/Invoices/PaymentMethods controllers still own the write actions the tabs post to.
/// </summary>
[Area("Provider")]
[Authorize(Policy = "Provider")]
public sealed class FinanceController : BaseController
{
    private readonly EarningsFacade _earnings;
    private readonly ProviderInvoicesFacade _invoices;
    private readonly ProviderPaymentMethodsFacade _methods;

    public FinanceController(
        EarningsFacade earnings,
        ProviderInvoicesFacade invoices,
        ProviderPaymentMethodsFacade methods)
    {
        _earnings = earnings;
        _invoices = invoices;
        _methods = methods;
    }

    // ── GET /provider/finance ─────────────────────────────────────────────────────
    [HttpGet("provider/finance")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["ProviderNav"] = "Finance";
        ViewBag.Sidebar = new ProviderSidebarVm { DisplayName = User.Identity?.Name ?? "Provider" };

        // API1: gather the three independent reads concurrently, then GuardSignOut each.
        var earningsTask = _earnings.GetEarningsAsync(ct);
        var invoicesTask = _invoices.GetAsync(ct);
        var methodsTask = _methods.GetAsync(ct);
        await Task.WhenAll(earningsTask, invoicesTask, methodsTask);

        var earnings = await earningsTask;
        if (GuardSignOut(earnings) is { } signOut1) return signOut1;
        var invoices = await invoicesTask;
        if (GuardSignOut(invoices) is { } signOut2) return signOut2;
        var methods = await methodsTask;
        if (GuardSignOut(methods) is { } signOut3) return signOut3;

        var vm = new ProviderFinanceVm
        {
            Earnings = earnings.Data ?? new EarningsVm(),
            Invoices = invoices.Data ?? new ProviderInvoicesVm(),
            Methods = methods.Data ?? new PaymentMethodsVm(),
        };

        // Surface a soft notice if any tab failed to load (degrade, don't 500 — ERR3).
        if (!earnings.IsSuccess || !invoices.IsSuccess || !methods.IsSuccess)
            SetError(earnings.Error ?? invoices.Error ?? methods.Error);

        return View(vm);
    }
}
