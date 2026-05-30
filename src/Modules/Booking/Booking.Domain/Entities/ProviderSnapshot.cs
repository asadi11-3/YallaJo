using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

/// <summary>
/// Booking-owned denormalized snapshot of a Provider.
/// Populated via inbox handlers listening to Accounts integration events.
/// </summary>
public sealed class ProviderSnapshot : BaseEntity
{
    private ProviderSnapshot()
    {
    }

    public Guid ProviderId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public BookingProviderStatus Status { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    public static ProviderSnapshot Create(
        Guid providerId,
        Guid ownerUserId,
        string displayName,
        BookingProviderStatus status)
    {
        return new ProviderSnapshot
        {
            Id = providerId, // Use ProviderId as PK for easy lookup
            ProviderId = providerId,
            OwnerUserId = ownerUserId,
            DisplayName = displayName,
            Status = status,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string displayName, BookingProviderStatus status)
    {
        DisplayName = displayName;
        Status = status;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
