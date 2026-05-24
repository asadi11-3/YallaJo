using Booking.Domain.Enums;
using Booking.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Exceptions;

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

    public static JoinRequest Create(
        Guid tourBookingId,
        Guid userId,
        int participantCount,
        string? message)
    {
        if (tourBookingId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("TourBookingId must be provided.");
        }

        if (userId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("UserId must be provided.");
        }

        if (participantCount < 1)
        {
            throw new BusinessRuleViolationException("ParticipantCount must be at least 1.");
        }

        var trimmedMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();

        var request = new JoinRequest
        {
            TourBookingId = tourBookingId,
            UserId = userId,
            ParticipantCount = participantCount,
            Message = trimmedMessage,
            Status = JoinRequestStatus.Pending,
        };

        request.AddDomainEvent(new JoinRequestCreatedDomainEvent(
            JoinRequestId: request.Id,
            TourBookingId: tourBookingId,
            UserId: userId));

        return request;
    }

    public void Approve()
    {
        if (Status != JoinRequestStatus.Pending)
        {
            throw new BusinessRuleViolationException(
                $"Cannot approve a join request in state {Status}.");
        }

        Status = JoinRequestStatus.Approved;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();

        AddDomainEvent(new JoinRequestApprovedDomainEvent(
            JoinRequestId: Id,
            TourBookingId: TourBookingId,
            UserId: UserId));
    }

    public void Reject(string? reason)
    {
        if (Status != JoinRequestStatus.Pending)
        {
            throw new BusinessRuleViolationException(
                $"Cannot reject a join request in state {Status}.");
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        Status = JoinRequestStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
        ResponseMessage = trimmed;
        MarkUpdated();

        AddDomainEvent(new JoinRequestRejectedDomainEvent(
            JoinRequestId: Id,
            TourBookingId: TourBookingId,
            UserId: UserId,
            Reason: trimmed ?? string.Empty));
    }
}
