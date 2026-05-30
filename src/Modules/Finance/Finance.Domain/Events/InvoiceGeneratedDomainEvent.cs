using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when an invoice has been generated as a result of a completed payment.
/// </summary>
/// <param name="InvoiceId">Identifier of the newly generated invoice aggregate.</param>
/// <param name="BookingId">Identifier of the tour booking.</param>
/// <param name="UserId">Identifier of the user (buyer).</param>
/// <param name="ProviderId">Identifier of the tour provider (seller).</param>
/// <param name="AmountTotal">Invoice total inclusive of tax and discounts.</param>
/// <param name="AmountSubtotal">Sum of line items before tax and discounts.</param>
/// <param name="AmountTax">Total tax applied.</param>
/// <param name="AmountDiscount">Total discounts applied.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
public sealed record InvoiceGeneratedDomainEvent(
    Guid InvoiceId,
    Guid BookingId,
    Guid UserId,
    Guid ProviderId,
    decimal AmountTotal,
    decimal AmountSubtotal,
    decimal AmountTax,
    decimal AmountDiscount,
    string Currency) : DomainEventBase;
