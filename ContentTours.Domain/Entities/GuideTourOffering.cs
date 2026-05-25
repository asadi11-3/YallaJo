using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

/// <summary>
/// Represents a tour guide's offering for a specific tour.
/// Replaces the simple TourTourGuide join entity with full lifecycle management.
/// </summary>
public sealed class GuideTourOffering : AuditableEntity
{
    private GuideTourOffering()
    {
    }

    public Guid TourId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public GuideOfferingStatus Status { get; private set; } = GuideOfferingStatus.Active;

    // Private tour variant
    public bool OffersPrivateTour { get; private set; }
    public decimal? PrivateTourPriceMultiplier { get; private set; }
    public decimal? PrivateTourFlatPrice { get; private set; }

    // Origin
    public bool IsProposer { get; private set; }
    public Guid? ApplicationId { get; private set; }
    public Guid? AssignedByUserId { get; private set; }

    // Suspension audit
    public string? SuspensionReason { get; private set; }
    public Guid? SuspendedByAdminId { get; private set; }

    public static GuideTourOffering Create(
        Guid tourId,
        Guid tourGuideId,
        bool isProposer = false,
        Guid? applicationId = null,
        Guid? assignedByUserId = null)
    {
        return new GuideTourOffering
        {
            TourId = tourId,
            TourGuideId = tourGuideId,
            Status = GuideOfferingStatus.Active,
            IsProposer = isProposer,
            ApplicationId = applicationId,
            AssignedByUserId = assignedByUserId
        };
    }

    public Result EnablePrivateTour(decimal? multiplier, decimal? flatPrice)
    {
        if (multiplier is null && flatPrice is null)
            return Result.Failure(new Error("GuideTourOffering.PrivatePriceRequired", "Either multiplier or flat price must be provided for private tours."));

        if (multiplier is not null && multiplier <= 0)
            return Result.Failure(new Error("GuideTourOffering.InvalidMultiplier", "Multiplier must be greater than zero."));

        if (flatPrice is not null && flatPrice <= 0)
            return Result.Failure(new Error("GuideTourOffering.InvalidFlatPrice", "Flat price must be greater than zero."));

        OffersPrivateTour = true;
        PrivateTourPriceMultiplier = multiplier;
        PrivateTourFlatPrice = flatPrice;
        MarkUpdated();
        return Result.Success();
    }

    public void DisablePrivateTour()
    {
        OffersPrivateTour = false;
        PrivateTourPriceMultiplier = null;
        PrivateTourFlatPrice = null;
        MarkUpdated();
    }

    public Result Suspend(Guid adminId, string reason)
    {
        if (Status == GuideOfferingStatus.Removed)
            return Result.Failure(new Error("GuideTourOffering.Removed", "Removed offerings cannot be suspended."));

        if (Status == GuideOfferingStatus.Suspended)
            return Result.Failure(new Error("GuideTourOffering.AlreadySuspended", "The offering is already suspended."));

        Status = GuideOfferingStatus.Suspended;
        SuspensionReason = reason.Trim();
        SuspendedByAdminId = adminId;
        MarkUpdated();
        return Result.Success();
    }

    public Result Reinstate()
    {
        if (Status != GuideOfferingStatus.Suspended)
            return Result.Failure(new Error("GuideTourOffering.NotSuspended", "Only suspended offerings can be reinstated."));

        Status = GuideOfferingStatus.Active;
        SuspensionReason = null;
        SuspendedByAdminId = null;
        MarkUpdated();
        return Result.Success();
    }

    public Result Remove()
    {
        if (Status == GuideOfferingStatus.Removed)
            return Result.Failure(new Error("GuideTourOffering.AlreadyRemoved", "The offering is already removed."));

        Status = GuideOfferingStatus.Removed;
        MarkUpdated();
        return Result.Success();
    }
}
