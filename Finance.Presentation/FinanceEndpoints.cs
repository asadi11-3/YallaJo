using Finance.Presentation.Endpoints.CommissionRule;
using Finance.Presentation.Endpoints.Invoice;
using Finance.Presentation.Endpoints.Payment;
using Finance.Presentation.Endpoints.Payout;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Finance.Presentation;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var paymentGroup = endpoints.MapGroup("/api/v1/payments");
        PaymentEndpoints.MapPaymentEndpoints(paymentGroup);

        var invoiceGroup = endpoints.MapGroup("/api/v1/invoices");
        InvoiceEndpoints.MapInvoiceEndpoints(invoiceGroup);

        var payoutGroup = endpoints.MapGroup("/api/v1/payouts");
        PayoutEndpoints.MapPayoutEndpoints(payoutGroup);

        var commissionGroup = endpoints.MapGroup("/api/v1/commissions");
        CommissionRuleEndpoints.MapCommissionRuleEndpoints(commissionGroup);

        return endpoints;
    }
}
