using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class JoinRequest : AuditableEntity, IAggregateRoot
{
    private JoinRequest() { } // EF Core

    public Guid TourBookingId { get; private set; }
    public Guid UserId { get; private set; }
    public JoinRequestStatus Status { get; private set; } = JoinRequestStatus.Pending;
    public string? Message { get; private set; }
    public int ParticipantCount { get; private set; } = 1;
    public DateTime? RespondedAt { get; private set; }
    public string? ResponseMessage { get; private set; }

    public TourBooking TourBooking { get; private set; } = default!;
}
