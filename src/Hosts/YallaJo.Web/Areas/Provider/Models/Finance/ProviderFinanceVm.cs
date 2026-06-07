using YallaJo.Web.Areas.Provider.Models.Earnings;
using YallaJo.Web.Areas.Provider.Models.Invoices;
using YallaJo.Web.Areas.Provider.Models.PaymentMethods;

namespace YallaJo.Web.Areas.Provider.Models.Finance;

/// <summary>
/// Composite VM for the unified §4.8 finance page (Payouts / Invoices / Methods tabs).
/// Aggregates the three existing finance sub-VMs via Pattern A (CONTROLLER_AUTHORING_GUIDE §9.5).
/// </summary>
public sealed class ProviderFinanceVm
{
    public EarningsVm Earnings { get; init; } = new();
    public ProviderInvoicesVm Invoices { get; init; } = new();
    public PaymentMethodsVm Methods { get; init; } = new();
}
