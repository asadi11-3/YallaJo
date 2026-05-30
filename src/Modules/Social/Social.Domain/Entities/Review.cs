using Social.Domain.Enums;
using Social.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>
/// A user-authored review for a tour, place, business, or tour-guide.
///
/// Rules enforced:
///   S-R1: BookingVerified derived from BookingEligibilitySnapshot (caller responsibility).
///   S-R2: One per (UserId, TargetType, TargetId) WHERE IsDeleted=0 — unique index in DB.
///   S-R3: 48-hour edit window; provider replies have no time limit.
///   S-R4: Profanity hit -> Status=AwaitingModeration, no ReviewPublishedDomainEvent.
///   S-R5: 3 unique reports -> AutoHide.
/// </summary>
public sealed class Review : AuditableEntity, IAggregateRoot
{
    private readonly List<ReviewReply> _replies = [];
    private Review() { } // EF Core

    // ── Identity ─────────────────────────────────────────────────────────────
    public Guid UserId                  { get; private set; }
    public ReviewTargetType TargetType  { get; private set; }
    public Guid TargetId                { get; private set; }

    // ── Content ──────────────────────────────────────────────────────────────
    /// <summary>Rating in 0.5 increments from 1.0 to 5.0 (S-R6).</summary>
    public decimal Rating               { get; private set; }
    public string? Title                { get; private set; }
    public string Content               { get; private set; } = string.Empty;
    public DateOnly? VisitDate          { get; private set; }

    // ── Status and flags ─────────────────────────────────────────────────────
    public ReviewStatus Status          { get; private set; } = ReviewStatus.Published;
    public bool IsVerifiedBooking       { get; private set; }
    public bool BookingVerified => IsVerifiedBooking;
    public bool ProfanityFlagged        { get; private set; }
    public DateTime? AutoHiddenAt       { get; private set; }
    public int CurrentReportCount       { get; private set; }
    public int ReportCount => CurrentReportCount;
    public int HelpfulVoteCount         { get; private set; }
    public DateTime? LastEditedAt       { get; private set; }

    // ── Children ──────────────────────────────────────────────────────────────
    public IReadOnlyCollection<ReviewReply> Replies => _replies.AsReadOnly();

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Review Create(
        Guid userId,
        ReviewTargetType targetType,
        Guid targetId,
        decimal rating,
        string? title,
        string content,
        DateOnly? visitDate,
        bool isVerifiedBooking,
        bool profanityDetected,
        TimeProvider timeProvider)
    {
        ValidateRating(rating);
        var now    = timeProvider.GetUtcNow().UtcDateTime;
        var status = profanityDetected ? ReviewStatus.AwaitingModeration : ReviewStatus.Published;

        var review = new Review
        {
            UserId            = userId,
            TargetType        = targetType,
            TargetId          = targetId,
            Rating            = rating,
            Title             = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            Content           = content,
            VisitDate         = visitDate,
            IsVerifiedBooking = isVerifiedBooking,
            ProfanityFlagged  = profanityDetected,
            Status            = status,
        };

        if (status == ReviewStatus.Published)
        {
            review.AddDomainEvent(new ReviewPublishedDomainEvent(
                review.Id, userId, targetType, targetId, rating, now));
        }

        return review;
    }

    // ── State transitions ─────────────────────────────────────────────────────

    public void Edit(
        decimal newRating,
        string? newTitle,
        string newContent,
        DateOnly? visitDate,
        TimeProvider timeProvider)
    {
        if (Status is ReviewStatus.DeletedByUser or ReviewStatus.RemovedByAdmin)
            throw new InvalidOperationException("Cannot edit a deleted review.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if ((now - CreatedAt).TotalHours > 48)
            throw new InvalidOperationException("Review.EditWindowExpired");

        ValidateRating(newRating);
        var oldRating = Rating;
        Rating        = newRating;
        Title         = string.IsNullOrWhiteSpace(newTitle) ? null : newTitle.Trim();
        Content       = newContent;
        VisitDate     = visitDate;
        LastEditedAt  = now;
        MarkUpdated();

        AddDomainEvent(new ReviewEditedDomainEvent(Id, UserId, oldRating, newRating, now));
    }

    public void Delete(ReviewDeletionSource source, TimeProvider timeProvider)
    {
        if (Status is ReviewStatus.DeletedByUser or ReviewStatus.RemovedByAdmin) return; // idempotent

        Status = source == ReviewDeletionSource.Admin
            ? ReviewStatus.RemovedByAdmin
            : ReviewStatus.DeletedByUser;
        SoftDelete();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        AddDomainEvent(new ReviewDeletedDomainEvent(Id, UserId, TargetType, TargetId, now, source));
    }

    public void AutoHide(int reportCount, TimeProvider timeProvider)
    {
        if (Status == ReviewStatus.AutoHidden) return; // idempotent

        var now            = timeProvider.GetUtcNow().UtcDateTime;
        Status             = ReviewStatus.AutoHidden;
        AutoHiddenAt       = now;
        CurrentReportCount = reportCount;
        MarkUpdated();

        AddDomainEvent(new ReviewAutoHiddenDomainEvent(Id, TargetType, TargetId, reportCount, now));
    }

    public void Restore(Guid adminUserId, TimeProvider timeProvider)
    {
        if (Status != ReviewStatus.AutoHidden)
            throw new InvalidOperationException("Can only restore auto-hidden reviews.");

        Status = ReviewStatus.Published;
        MarkUpdated();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        AddDomainEvent(new ReviewRestoredDomainEvent(Id, adminUserId, now));
    }

    public void IncrementReportCount() { CurrentReportCount++; MarkUpdated(); }

    public bool HideIfReportThresholdReached(TimeProvider timeProvider, int threshold = 3)
    {
        if (CurrentReportCount < threshold)
            return false;

        AutoHide(CurrentReportCount, timeProvider);
        return true;
    }

    public void IncrementHelpfulVotes()
    {
        HelpfulVoteCount++;
        MarkUpdated();
    }

    public void DecrementHelpfulVotes()
    {
        if (HelpfulVoteCount == 0) return;
        HelpfulVoteCount--;
        MarkUpdated();
    }

    // ── Reply management ─────────────────────────────────────────────────────

    public ReviewReply AddReply(Guid providerUserId, string content, TimeProvider timeProvider)
    {
        var now   = timeProvider.GetUtcNow().UtcDateTime;
        var reply = new ReviewReply(Id, providerUserId, content, now);
        _replies.Add(reply);
        AddDomainEvent(new ReviewReplyAddedDomainEvent(Id, reply.Id, providerUserId, now));
        return reply;
    }

    public void UpdateReply(Guid replyId, string newContent, Guid callerUserId)
    {
        var reply = _replies.FirstOrDefault(r => r.Id == replyId)
            ?? throw new KeyNotFoundException($"Reply {replyId} not found on review {Id}.");
        reply.Update(newContent, callerUserId);
        MarkUpdated();
    }

    public void DeleteReply(Guid replyId, Guid callerUserId)
    {
        var reply = _replies.FirstOrDefault(r => r.Id == replyId);
        if (reply is null) return; // idempotent
        reply.Delete(callerUserId);
        MarkUpdated();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    public static bool IsHalfStarRating(decimal rating)
        => rating >= 0.5m && rating <= 5.0m && (rating * 2) % 1 == 0;

    private static void ValidateRating(decimal rating)
    {
        if (!IsHalfStarRating(rating))
            throw new ArgumentOutOfRangeException(nameof(rating),
                $"Rating must be 0.5-5.0 in 0.5 increments, got {rating}.");
    }
}
