namespace YallaJo.Web.Areas.Accounts.Models.Payments;

public sealed class PaymentsVm
{
    public IReadOnlyList<PaymentRowVm> Payments { get; init; } = [];

    public bool HasPayments => Payments.Count > 0;
}

public sealed record PaymentRowVm(
    Guid Id,
    Guid? BookingId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string PaymentType,
    string Status,
    decimal RefundedTotal,
    DateTime? PaidAt,
    DateTime CreatedAt)
{
    public bool IsRefunded => RefundedTotal > 0;
}
