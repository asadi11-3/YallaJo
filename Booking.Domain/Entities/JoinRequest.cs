using Booking.Domain.Enums;
using Booking.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Domain.Entities;

public sealed class JoinRequest : AuditableEntity, IAggregateRoot
{
    private JoinRequest() { } // EF Core

    // === Identity / relationships ===
    public Guid TourBookingId { get; private set; }
    public Guid AvailabilitySlotId { get; private set; }
    public Guid UserId { get; private set; }

    // === State ===
    public JoinRequestStatus Status { get; private set; } = JoinRequestStatus.Pending;
    public string? Message { get; private set; }
    public int ParticipantCount { get; private set; } = 1;

    // === Expiry ===
    /// <summary>48h TTL from creation per spec.</summary>
    public DateTime ExpiresAt { get; private set; }

    // === Resolution ===
    public DateTime? RespondedAt { get; private set; }
    public string? ResponseMessage { get; private set; }

    /// <summary>Non-null when approved and a new booking was created for the joiner.</summary>
    public Guid? ResultingBookingId { get; private set; }

    public TourBooking TourBooking { get; private set; } = default!;

    // ===  Factory ===

    public static Result<JoinRequest> Create(
        Guid tourBookingId,
        Guid availabilitySlotId,
        Guid userId,
        int participantCount,
        string? message,
        DateTime utcNow)
    {
        if (participantCount < 1)
        {
            return Result.Failure<JoinRequest>(Error.Validation("JoinRequest.InvalidParticipantCount", "Participant count must be at least 1."));
        }

        var request = new JoinRequest
        {
            Id = Guid.NewGuid(),
            TourBookingId = tourBookingId,
            AvailabilitySlotId = availabilitySlotId,
            UserId = userId,
            ParticipantCount = participantCount,
            Message = message,
            Status = JoinRequestStatus.Pending,
            ExpiresAt = utcNow.AddHours(48),
            CreatedAt = utcNow,
        };

        request.AddDomainEvent(new JoinRequestCreatedDomainEvent(request.Id, tourBookingId, userId));
        return Result.Success(request);
    }

    // === Domain methods ===

    public Result Approve(Guid respondedByUserId, string? responseMessage, DateTime utcNow)
    {
        if (Status != JoinRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("JoinRequest.NotPending", "Only pending join requests can be approved."));
        }

        if (IsExpired(utcNow))
        {
            return Result.Failure(Error.Conflict("JoinRequest.Expired", "This join request has expired."));
        }

        Status = JoinRequestStatus.Approved;
        RespondedAt = utcNow;
        ResponseMessage = responseMessage;
        MarkUpdated();

        AddDomainEvent(new JoinRequestApprovedDomainEvent(Id, TourBookingId, UserId));
        return Result.Success();
    }

    public Result Reject(Guid respondedByUserId, string? responseMessage, DateTime utcNow)
    {
        if (Status != JoinRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("JoinRequest.NotPending", "Only pending join requests can be rejected."));
        }

        Status = JoinRequestStatus.Rejected;
        RespondedAt = utcNow;
        ResponseMessage = responseMessage;
        MarkUpdated();

        AddDomainEvent(new JoinRequestRejectedDomainEvent(Id, TourBookingId, UserId, responseMessage ?? string.Empty));
        return Result.Success();
    }

    public Result Expire(DateTime utcNow)
    {
        if (Status != JoinRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("JoinRequest.NotPending", "Only pending join requests can expire."));
        }

        Status = JoinRequestStatus.Expired;
        MarkUpdated();

        AddDomainEvent(new JoinRequestExpiredDomainEvent(Id, TourBookingId, UserId));
        return Result.Success();
    }

    public Result AttachResultingBooking(Guid bookingId)
    {
        if (Status != JoinRequestStatus.Approved)
        {
            return Result.Failure(Error.Conflict("JoinRequest.NotApproved", "Can only attach a resulting booking to an approved join request."));
        }

        ResultingBookingId = bookingId;
        MarkUpdated();
        return Result.Success();
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt && Status == JoinRequestStatus.Pending;
}
