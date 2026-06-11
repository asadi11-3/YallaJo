namespace Booking.Application.Queries.GetProviderBookingStats;

/// <summary>
/// [Backend] B5: per-status booking counts for a provider.
/// Pending aggregates AwaitingPayment + PendingConfirmation;
/// Total is the count of all bookings regardless of status.
/// </summary>
public sealed record ProviderBookingStatsDto(
    int Total,
    int Pending,
    int Confirmed,
    int Completed,
    int Cancelled,
    int Rejected);
