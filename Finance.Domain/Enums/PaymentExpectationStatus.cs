namespace Finance.Domain.Enums;

/// <summary>
/// Lifecycle of a Finance-local payment expectation seeded from a Booking integration event.
/// Used to bridge "booking created" -> "payment initiated" without race conditions.
/// </summary>
public enum PaymentExpectationStatus : byte
{
    /// <summary>Booking emitted, awaiting first POST /payments/initiate.</summary>
    AwaitingPayment = 0,

    /// <summary>A Payment row has been created (gateway initiation completed).</summary>
    Paid = 1,

    /// <summary>Booking was cancelled or expired before any Payment was initiated.</summary>
    Cancelled = 2,
}
