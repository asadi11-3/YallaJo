using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

/// <summary>
/// Booking-owned denormalized snapshot of a Tour.
/// Populated via inbox handlers listening to ContentTours integration events.
/// Avoids cross-module DB joins during booking creation.
/// </summary>
public sealed class TourSnapshot : BaseEntity
{
    private TourSnapshot()
    {
    }

    public Guid TourId { get; private set; }
    public Guid ProviderId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal BasePrice { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsApproved { get; private set; }
    public bool IsInstantBooking { get; private set; }
    public Guid? RefundPolicyId { get; private set; }
    public string? RefundPolicySnapshotJson { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    public static TourSnapshot Create(
        Guid tourId,
        Guid providerId,
        string title,
        string currency,
        decimal basePrice,
        bool isActive,
        bool isApproved,
        bool isInstantBooking,
        Guid? refundPolicyId = null,
        string? refundPolicySnapshotJson = null)
    {
        return new TourSnapshot
        {
            Id = tourId, // Use TourId as PK for easy lookup
            TourId = tourId,
            ProviderId = providerId,
            Title = title,
            Currency = currency,
            BasePrice = basePrice,
            IsActive = isActive,
            IsApproved = isApproved,
            IsInstantBooking = isInstantBooking,
            RefundPolicyId = refundPolicyId,
            RefundPolicySnapshotJson = refundPolicySnapshotJson,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string currency,
        decimal basePrice,
        bool isActive,
        bool isApproved,
        bool isInstantBooking)
    {
        Title = title;
        Currency = currency;
        BasePrice = basePrice;
        IsActive = isActive;
        IsApproved = isApproved;
        IsInstantBooking = isInstantBooking;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void MarkInactive()
    {
        IsActive = false;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
