using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

/// <summary>
/// Represents a guide-proposed new tour concept.
/// On approval, creates a Tour + GuideTourOffering with shared platform ownership.
/// </summary>
public sealed class TourProposal : AuditableEntity, IAggregateRoot
{
    private TourProposal()
    {
    }

    public Guid TourGuideId { get; private set; }
    public Guid GuideUserId { get; private set; }
    public TourProposalStatus Status { get; private set; } = TourProposalStatus.Draft;

    // Proposal content
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public Guid PlaceId { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MaxGroupSize { get; private set; }
    public decimal BasePrice { get; private set; }
    public string Currency { get; private set; } = "JOD";

    // Exclusivity preference (guide decides at proposal time)
    public bool RequestExclusive { get; private set; }

    // Review
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // On approval — the Tour created from this proposal
    public Guid? CreatedTourId { get; private set; }

    public static Result<TourProposal> Create(
        Guid tourGuideId,
        Guid guideUserId,
        string title,
        string description,
        string shortDescription,
        Guid placeId,
        int durationMinutes,
        int maxGroupSize,
        decimal basePrice,
        string currency,
        bool requestExclusive)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            return Result.Failure<TourProposal>(new Error("TourProposal.InvalidTitle", "Title is required and cannot exceed 200 characters."));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<TourProposal>(new Error("TourProposal.DescriptionRequired", "Description is required."));

        if (string.IsNullOrWhiteSpace(shortDescription) || shortDescription.Trim().Length > 500)
            return Result.Failure<TourProposal>(new Error("TourProposal.InvalidShortDescription", "Short description is required and cannot exceed 500 characters."));

        if (placeId == Guid.Empty)
            return Result.Failure<TourProposal>(new Error("TourProposal.PlaceRequired", "A valid PlaceId is required."));

        if (durationMinutes is <= 0 or > 2880)
            return Result.Failure<TourProposal>(new Error("TourProposal.InvalidDuration", "Duration must be between 1 and 2880 minutes."));

        if (maxGroupSize is <= 0 or > 100)
            return Result.Failure<TourProposal>(new Error("TourProposal.InvalidGroupSize", "Max group size must be between 1 and 100."));

        if (basePrice <= 0)
            return Result.Failure<TourProposal>(new Error("TourProposal.InvalidPrice", "Base price must be greater than zero."));

        return Result.Success(new TourProposal
        {
            TourGuideId = tourGuideId,
            GuideUserId = guideUserId,
            Status = TourProposalStatus.Draft,
            Title = title.Trim(),
            Description = description.Trim(),
            ShortDescription = shortDescription.Trim(),
            PlaceId = placeId,
            DurationMinutes = durationMinutes,
            MaxGroupSize = maxGroupSize,
            BasePrice = basePrice,
            Currency = currency.Trim().ToUpperInvariant(),
            RequestExclusive = requestExclusive
        });
    }

    public Result Submit()
    {
        if (Status != TourProposalStatus.Draft)
            return Result.Failure(new Error("TourProposal.NotDraft", "Only draft proposals can be submitted."));

        Status = TourProposalStatus.Submitted;
        MarkUpdated();
        return Result.Success();
    }

    public Result Approve(Guid adminId, Guid createdTourId, DateTime utcNow)
    {
        if (Status != TourProposalStatus.Submitted)
            return Result.Failure(new Error("TourProposal.NotSubmitted", "Only submitted proposals can be approved."));

        Status = TourProposalStatus.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAt = utcNow;
        CreatedTourId = createdTourId;
        MarkUpdated();
        return Result.Success();
    }

    public Result Reject(Guid adminId, string reason, DateTime utcNow)
    {
        if (Status != TourProposalStatus.Submitted)
            return Result.Failure(new Error("TourProposal.NotSubmitted", "Only submitted proposals can be rejected."));

        Status = TourProposalStatus.Rejected;
        ReviewedByAdminId = adminId;
        ReviewedAt = utcNow;
        RejectionReason = reason.Trim();
        MarkUpdated();
        return Result.Success();
    }

    public Result Update(
        string title,
        string description,
        string shortDescription,
        Guid placeId,
        int durationMinutes,
        int maxGroupSize,
        decimal basePrice,
        string currency,
        bool requestExclusive)
    {
        if (Status != TourProposalStatus.Draft && Status != TourProposalStatus.Rejected)
            return Result.Failure(new Error("TourProposal.CannotUpdate", "Only draft or rejected proposals can be updated."));

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            return Result.Failure(new Error("TourProposal.InvalidTitle", "Title is required and cannot exceed 200 characters."));

        Title = title.Trim();
        Description = description.Trim();
        ShortDescription = shortDescription.Trim();
        PlaceId = placeId;
        DurationMinutes = durationMinutes;
        MaxGroupSize = maxGroupSize;
        BasePrice = basePrice;
        Currency = currency.Trim().ToUpperInvariant();
        RequestExclusive = requestExclusive;
        MarkUpdated();
        return Result.Success();
    }
}
