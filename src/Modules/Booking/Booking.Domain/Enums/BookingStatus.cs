namespace Booking.Domain.Enums;

/// <summary>
/// Status of a booking aggregate (TourBooking, PackageBooking, Reservation).
///
/// Legacy statuses (0..6) are retained for PackageBooking + Reservation.
/// TourBooking uses the engine-era statuses (10..15) introduced with the
/// Booking Engine sprint (TASK 4 / TASK 5).
/// </summary>
public enum BookingStatus : byte
{
    // ── Legacy (PackageBooking, Reservation) ───────────────────────────────
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Refunded = 5,
    NoShow = 6,

    // ── TourBooking engine state machine ──────────────────────────────────
    /// <summary>Booking created, payment not yet received (10-min TTL).</summary>
    AwaitingPayment = 10,
    /// <summary>Payment received on non-instant tour; provider must confirm within 24h.</summary>
    PendingConfirmation = 11,
    /// <summary>Provider rejected a pending booking — triggers automatic full refund.</summary>
    Rejected = 12,

    // ── Phase 3: Dispute lifecycle (G4a) ──────────────────────────────────
    /// <summary>
    /// User opened a dispute on a Completed booking within the 48-hour window.
    /// Admin must Resolve to clear.
    /// </summary>
    Disputed = 13,
    /// <summary>
    /// Admin resolved a Disputed booking. Terminal state for the dispute lifecycle.
    /// Resolution notes captured on the aggregate; downstream consumers (Finance, Messaging)
    /// react via the tour-booking.dispute-resolved.v1 integration event.
    /// </summary>
    Resolved = 14
}
