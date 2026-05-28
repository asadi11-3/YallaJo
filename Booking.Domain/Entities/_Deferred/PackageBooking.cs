using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Booking.Domain.Entities;

public sealed class PackageBooking : AuditableEntity
{
    private PackageBooking() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid TourPackageId { get; private set; }
    public DateOnly BookingDate { get; private set; }
    public int ParticipantCount { get; private set; } = 1;
    public Money TotalPrice { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public string? SpecialRequests { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
}
