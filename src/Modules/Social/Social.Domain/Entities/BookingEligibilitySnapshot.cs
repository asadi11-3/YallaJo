using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>
/// Denormalized snapshot of a user's completed bookings per reviewable entity.
/// Used to verify whether a reviewer has a recent completed booking (S-R1 within 30 days).
/// Updated by BookingTourBookingCompletedIntegrationEvent inbox handler.
/// </summary>
public sealed class BookingEligibilitySnapshot : BaseEntity
{
    private BookingEligibilitySnapshot() { } // EF Core

    public BookingEligibilitySnapshot(
        Guid userId,
        ReviewTargetType targetType,
        Guid targetId,
        DateTime firstCompletedAt)
    {
        UserId              = userId;
        TargetType          = targetType;
        TargetId            = targetId;
        FirstCompletedAt    = firstCompletedAt;
        LastCompletedAt     = firstCompletedAt;
        CompletedBookingCount = 1;
    }

    public Guid UserId                  { get; private set; }
    public ReviewTargetType TargetType  { get; private set; }
    public Guid TargetId                { get; private set; }
    public DateTime FirstCompletedAt    { get; private set; }
    public DateTime LastCompletedAt     { get; private set; }
    public int CompletedBookingCount    { get; private set; }

    public void RecordBooking(DateTime completedAt)
    {
        LastCompletedAt = completedAt;
        CompletedBookingCount++;
    }

    /// <summary>Returns true if the last completed booking is within the eligibility window.</summary>
    public bool IsEligibleForVerifiedReview(DateTime asOf, int windowDays = 30)
        => (asOf - LastCompletedAt).TotalDays <= windowDays;
}
