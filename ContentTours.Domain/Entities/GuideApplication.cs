using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

/// <summary>
/// Represents a guide's application to run an existing tour.
/// </summary>
public sealed class GuideApplication : AuditableEntity, IAggregateRoot
{
    private const int MaxResubmissions = 2;

    private GuideApplication()
    {
    }

    public Guid TourId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public Guid GuideUserId { get; private set; }
    public GuideApplicationStatus Status { get; private set; } = GuideApplicationStatus.Draft;

    public string Message { get; private set; } = string.Empty;
    public string? ProposedScheduleJson { get; private set; }
    public decimal? ProposedBasePrice { get; private set; }
    public string? RelevantExperience { get; private set; }

    public int ResubmissionCount { get; private set; }

    // Review
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public static Result<GuideApplication> Create(
        Guid tourId,
        Guid tourGuideId,
        Guid guideUserId,
        string message,
        string? relevantExperience = null,
        decimal? proposedBasePrice = null)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length > 2000)
            return Result.Failure<GuideApplication>(new Error("GuideApplication.InvalidMessage", "Message is required and cannot exceed 2000 characters."));

        if (proposedBasePrice.HasValue && proposedBasePrice.Value <= 0)
            return Result.Failure<GuideApplication>(new Error("GuideApplication.InvalidPrice", "Proposed price must be greater than zero."));

        return Result.Success(new GuideApplication
        {
            TourId = tourId,
            TourGuideId = tourGuideId,
            GuideUserId = guideUserId,
            Status = GuideApplicationStatus.Draft,
            Message = message.Trim(),
            RelevantExperience = string.IsNullOrWhiteSpace(relevantExperience) ? null : relevantExperience.Trim(),
            ProposedBasePrice = proposedBasePrice
        });
    }

    public Result Submit()
    {
        if (Status != GuideApplicationStatus.Draft)
            return Result.Failure(new Error("GuideApplication.NotDraft", "Only draft applications can be submitted."));

        Status = GuideApplicationStatus.Submitted;
        MarkUpdated();
        return Result.Success();
    }

    public Result Approve(Guid adminId, DateTime utcNow)
    {
        if (Status != GuideApplicationStatus.Submitted)
            return Result.Failure(new Error("GuideApplication.NotSubmitted", "Only submitted applications can be approved."));

        Status = GuideApplicationStatus.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAt = utcNow;
        MarkUpdated();
        return Result.Success();
    }

    public Result Reject(Guid adminId, string reason, DateTime utcNow)
    {
        if (Status != GuideApplicationStatus.Submitted)
            return Result.Failure(new Error("GuideApplication.NotSubmitted", "Only submitted applications can be rejected."));

        Status = GuideApplicationStatus.Rejected;
        ReviewedByAdminId = adminId;
        ReviewedAt = utcNow;
        RejectionReason = reason.Trim();
        MarkUpdated();
        return Result.Success();
    }

    public Result Resubmit(string message, string? relevantExperience, decimal? proposedBasePrice)
    {
        if (Status != GuideApplicationStatus.Rejected)
            return Result.Failure(new Error("GuideApplication.NotRejected", "Only rejected applications can be resubmitted."));

        if (ResubmissionCount >= MaxResubmissions)
            return Result.Failure(new Error("GuideApplication.ResubmitLimitReached", $"Maximum resubmission limit ({MaxResubmissions}) has been reached."));

        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length > 2000)
            return Result.Failure(new Error("GuideApplication.InvalidMessage", "Message is required and cannot exceed 2000 characters."));

        Message = message.Trim();
        RelevantExperience = string.IsNullOrWhiteSpace(relevantExperience) ? null : relevantExperience.Trim();
        ProposedBasePrice = proposedBasePrice;
        Status = GuideApplicationStatus.Submitted;
        ReviewedByAdminId = null;
        ReviewedAt = null;
        RejectionReason = null;
        ResubmissionCount++;
        MarkUpdated();
        return Result.Success();
    }
}
