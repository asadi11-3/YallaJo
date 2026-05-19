using Booking.Domain.Enums;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// Context passed to <see cref="Entities.TourBooking.Cancel"/> describing who initiated the
/// cancellation and whether the refund policy must be overridden to 100%.
/// </summary>
/// <param name="Source">Logical actor initiating the cancellation.</param>
/// <param name="Reason">Free-text reason (required for Provider; optional for User).</param>
/// <param name="ProviderInitiated">True when the provider (not the user) cancels; forces 100% refund.</param>
/// <param name="ForceMajeureOverride">True for admin force-refund flows; forces 100% refund.</param>
public sealed record BookingCancellationContext(
    CancellationSource Source,
    string? Reason,
    bool ProviderInitiated,
    bool ForceMajeureOverride);
