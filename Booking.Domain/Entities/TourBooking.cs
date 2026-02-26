using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class TourBooking : AuditableEntity, IAggregateRoot
{
    private readonly List<JoinRequest> _joinRequests = [];

    private TourBooking() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid TourId { get; private set; }
    public Guid? TourGuideId { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public int ParticipantCount { get; private set; } = 1;
    public decimal TotalPrice { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public string? SpecialRequests { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    public IReadOnlyCollection<JoinRequest> JoinRequests => _joinRequests.AsReadOnly();
}
