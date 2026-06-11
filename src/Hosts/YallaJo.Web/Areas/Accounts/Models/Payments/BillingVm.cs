using YallaJo.Web.Areas.Accounts.Models.Disputes;
using YallaJo.Web.Areas.Accounts.Models.Invoices;

namespace YallaJo.Web.Areas.Accounts.Models.Payments;

/// <summary>
/// Phase 3 (Accounts plan): Billing hub composing Payments + Invoices + Disputes
/// as tabs on /accounts/payments. Invoices/Disputes load best-effort (empty VMs on failure).
/// </summary>
public sealed class BillingVm
{
    public PaymentsVm Payments { get; init; } = new();
    public InvoicesVm Invoices { get; init; } = new();
    public DisputesVm Disputes { get; init; } = new();
    public string ActiveTab { get; init; } = "payments";
}
