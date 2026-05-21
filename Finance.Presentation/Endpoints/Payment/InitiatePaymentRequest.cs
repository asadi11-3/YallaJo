using Finance.Application.Commands.InitiatePayment;
using Finance.Domain.Enums;

namespace Finance.Presentation.Endpoints.Payment;

/// <summary>
/// HTTP body for POST /payments/initiate.
/// </summary>
public sealed record InitiatePaymentRequest(
    Guid BookingId,
    PaymentMethod PaymentMethod,
    string ReturnUrl)
{
    public InitiatePaymentCommand ToCommand(Guid callerUserId) => new(
        BookingId,
        PaymentMethod,
        ReturnUrl,
        callerUserId);
}
