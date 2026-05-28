using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class Reservation : AuditableEntity, IAggregateRoot
{
    private Reservation() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid? AvailabilitySlotId { get; private set; }
    public Guid? ServiceItemId { get; private set; }
    public DateOnly ReservationDate { get; private set; }
    public TimeOnly ReservationTime { get; private set; }
    public int PartySize { get; private set; } = 1;
    public decimal TotalPrice { get; private set; }
    public string TotalPriceCurrency { get; private set; } = "JOD";
    public string Currency { get; private set; } = "JOD";
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public string? SpecialRequests { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
}
