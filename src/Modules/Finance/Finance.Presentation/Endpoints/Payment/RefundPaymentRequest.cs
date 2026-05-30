namespace Finance.Presentation.Endpoints.Payment;

public sealed record RefundPaymentRequest(decimal Amount, string Currency, string Reason);
