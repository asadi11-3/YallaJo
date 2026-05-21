using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class PaymentSnapshot : BaseEntity
{
    private PaymentSnapshot() { }
    public Guid PaymentId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal CommissionAmount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public string Status { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public DateTime CompletedAt { get; private set; }

    public static PaymentSnapshot Completed(Guid paymentId, Guid bookingId, Guid userId, Guid providerId, decimal amount, string currency, DateTime completedAt)
        => new() { PaymentId = paymentId, BookingId = bookingId, UserId = userId, ProviderId = providerId, Amount = amount, Currency = currency, Status = "Completed", Type = "Payment", CompletedAt = completedAt };

    public static PaymentSnapshot Payout(Guid payoutId, Guid providerId, decimal amount, string currency, DateTime completedAt)
        => new() { PaymentId = payoutId, ProviderId = providerId, Amount = amount, Currency = currency, Status = "Completed", Type = "Payout", CompletedAt = completedAt };

    public static PaymentSnapshot Refund(Guid refundId, Guid originalPaymentId, Guid bookingId, decimal amount, string currency, DateTime completedAt)
        => new() { PaymentId = refundId, BookingId = bookingId, Amount = amount, Currency = currency, Status = "Completed", Type = "Refund", CompletedAt = completedAt };
}
