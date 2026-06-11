using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Disputes;
using YallaJo.Web.Areas.Accounts.Models.Invoices;
using YallaJo.Web.Areas.Accounts.Models.Payments;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Billing hub (Phase 3 of the Accounts master plan): one page hosting the Payments,
/// Invoices and Disputes tabs. The old /accounts/invoices and /accounts/disputes pages
/// permanently redirect here.
/// </summary>
[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.Payment.Read)]
public sealed class PaymentsController : BaseController
{
    private readonly PaymentsFacade _payments;
    private readonly InvoicesFacade _invoices;
    private readonly DisputesFacade _disputes;
    private readonly ProfileFacade _profile;

    public PaymentsController(
        PaymentsFacade payments,
        InvoicesFacade invoices,
        DisputesFacade disputes,
        ProfileFacade profile)
    {
        _payments = payments;
        _invoices = invoices;
        _disputes = disputes;
        _profile = profile;
    }

    [HttpGet("accounts/payments")]
    public async Task<IActionResult> Index(string? tab, int page = 1, CancellationToken ct = default)
    {
        ViewData["AccountNav"] = "Payments";
        await PopulateSidebarAsync(ct);

        var safePage = page < 1 ? 1 : page;

        // Compose all three tabs in parallel (UI-PERF-API1).
        var paymentsTask = _payments.GetAsync(ct);
        var invoicesTask = _invoices.GetAsync(ct);
        var disputesTask = _disputes.GetAsync(safePage, ct);
        await Task.WhenAll(paymentsTask, invoicesTask, disputesTask);

        var paymentsResult = paymentsTask.Result;
        if (GuardSignOut(paymentsResult) is { } signOut) return signOut;

        if (!paymentsResult.IsSuccess || paymentsResult.Data is null)
        {
            SetError(paymentsResult.Error);
        }

        // Invoices/disputes are best-effort: the API stays authoritative on permissions,
        // and a failed secondary tab must not break the whole Billing page (UI-ERR-ERR3).
        var invoicesResult = invoicesTask.Result;
        var disputesResult = disputesTask.Result;

        var vm = new BillingVm
        {
            Payments = paymentsResult.Data ?? new PaymentsVm(),
            Invoices = invoicesResult is { IsSuccess: true, Data: { } inv } ? inv : new InvoicesVm(),
            Disputes = disputesResult is { IsSuccess: true, Data: { } disp }
                ? disp
                : new DisputesVm { PageNumber = safePage },
            ActiveTab = NormalizeTab(tab),
        };

        return View(vm);
    }

    private static string NormalizeTab(string? tab) => tab?.ToLowerInvariant() switch
    {
        "invoices" => "invoices",
        "disputes" => "disputes",
        _ => "payments",
    };

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
