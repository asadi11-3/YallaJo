using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.invoice.generated.v1</c>) when an invoice has
/// been generated against a completed payment. Consumers: Messaging (notify buyer
/// and provider), Analytics.
/// </summary>
public sealed record InvoiceGeneratedIntegrationEvent(
    Guid InvoiceId,
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    Guid ProviderId,
    string InvoiceNumber,
    decimal AmountTotal,
    decimal AmountSubtotal,
    decimal AmountTax,
    decimal AmountDiscount,
    string Currency,
    DateTime IssuedAt) : IntegrationEventBase;
