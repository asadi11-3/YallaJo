namespace Booking.Domain.Enums;

/// <summary>
/// What caused a booking to move into <see cref="BookingStatus.Confirmed"/>.
/// </summary>
public enum ConfirmationSource : byte
{
    /// <summary>Instant booking: confirmed automatically by payment webhook.</summary>
    PaymentWebhook = 0,
    /// <summary>Provider explicitly called POST /confirm.</summary>
    Manual = 1,
    /// <summary>Provider did not act within 24h; ProviderAutoAcceptService kicked in.</summary>
    AutoAccept = 2
}
