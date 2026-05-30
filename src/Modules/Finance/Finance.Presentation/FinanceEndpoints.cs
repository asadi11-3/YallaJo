using Finance.Presentation.Endpoints.CommissionRule;
using Finance.Presentation.Endpoints.Dispute;
using Finance.Presentation.Endpoints.Earnings;
using Finance.Presentation.Endpoints.Invoice;
using Finance.Presentation.Endpoints.Payment;
using Finance.Presentation.Endpoints.Payout;
using Finance.Presentation.Endpoints.ProviderPaymentMethod;
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

        var providerPaymentMethodsGroup = endpoints.MapGroup("/api/v1/provider-payment-methods");
        ProviderPaymentMethodEndpoints.MapProviderPaymentMethodEndpoints(providerPaymentMethodsGroup);

        var disputeGroup = endpoints.MapGroup("/api/v1/disputes");
        DisputeEndpoints.MapDisputeEndpoints(disputeGroup);

        var earningsGroup = endpoints.MapGroup("/api/v1/finance");
        EarningsEndpoints.MapEarningsEndpoints(earningsGroup);

        return endpoints;
    }
}
