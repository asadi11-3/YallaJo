using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

/// <summary>
/// Finance-local snapshot of a Booking-created event. Materialised by the
/// <c>booking.tour-booking.created.v1</c> inbox handler so POST /payments/initiate
/// has a deterministic anchor (no remote calls back into Booking module).
/// Not an aggregate root — pure read-side projection.
/// </summary>
public sealed class PaymentExpectation : BaseEntity
{
    private PaymentExpectation() { } // EF Core

    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid TourId { get; private set; }

    public Money ExpectedAmount { get; private set; } = default!;
    public string Currency { get; private set; } = string.Empty;
    public string Reference { get; private set; } = string.Empty;

    public PaymentExpectationStatus Status { get; private set; } = PaymentExpectationStatus.AwaitingPayment;
    public Guid? PaymentId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Factory invoked from the Booking integration event handler.
    /// </summary>
    public static PaymentExpectation Create(
        Guid bookingId,
        Guid userId,
        Guid providerId,
        Guid tourId,
        Money expectedAmount,
        string reference,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(expectedAmount);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (expectedAmount.Amount <= 0m)
        {
            throw new ArgumentException("Expected amount must be positive.", nameof(expectedAmount));
        }

        return new PaymentExpectation
        {
            BookingId = bookingId,
            UserId = userId,
            ProviderId = providerId,
            TourId = tourId,
            ExpectedAmount = expectedAmount,
            Currency = expectedAmount.Currency,
            Reference = reference,
            Status = PaymentExpectationStatus.AwaitingPayment,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
    }

    /// <summary>Mark expectation as fulfilled when a Payment row is created.</summary>
    public void MarkPaid(Guid paymentId)
    {
        if (Status != PaymentExpectationStatus.AwaitingPayment)
        {
            return; // idempotent
        }

        PaymentId = paymentId;
        Status = PaymentExpectationStatus.Paid;
    }

    /// <summary>Mark expectation as cancelled if booking was cancelled before payment.</summary>
    public void Cancel()
    {
        if (Status != PaymentExpectationStatus.AwaitingPayment)
        {
            return; // idempotent
        }

        Status = PaymentExpectationStatus.Cancelled;
    }
}
