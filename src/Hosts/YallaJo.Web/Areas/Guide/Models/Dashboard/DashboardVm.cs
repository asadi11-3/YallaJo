namespace YallaJo.Web.Areas.Guide.Models.Dashboard;

public sealed class DashboardVm
{
    public string DisplayName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public int YearsOfExperience { get; init; }
    public bool HasFirstAid { get; init; }

    public decimal TotalEarned { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal PendingPayout { get; init; }
    public decimal NetEarnings { get; init; }
    public string EarningsCurrency { get; init; } = string.Empty;

    public int ActiveAvailabilityBlocks { get; init; }
    public IReadOnlyList<RecentTourVm> RecentTours { get; init; } = [];

    public TierProgressVm? TierProgress { get; init; }

    public bool HasActivity => RecentTours.Count > 0;
}

public sealed class TierProgressVm
{
    public string CurrentTier { get; init; } = string.Empty;
    public string? NextTier { get; init; }
    public int CompletedTours { get; init; }
    public int CompletedToursRequired { get; init; }
    public decimal AverageRating { get; init; }
    public decimal AverageRatingRequired { get; init; }
    public decimal ReportRate { get; init; }
    public decimal MaxReportRateAllowed { get; init; }
    public int ActiveMonths { get; init; }
    public int ActiveMonthsRequired { get; init; }
    public decimal CurrentCommissionRate { get; init; }

    public bool HasNextTier => !string.IsNullOrWhiteSpace(NextTier);

    public int ToursPercent => Percent(CompletedTours, CompletedToursRequired);
    public int RatingPercent => Percent(AverageRating, AverageRatingRequired);
    public int ActiveMonthsPercent => Percent(ActiveMonths, ActiveMonthsRequired);

    // Report rate is an "at or below" requirement: full bar when at/under the cap.
    public int ReportRatePercent => MaxReportRateAllowed <= 0
        ? 100
        : Math.Clamp((int)Math.Round((1 - (ReportRate / MaxReportRateAllowed)) * 100), 0, 100);

    public bool ReportRateOk => ReportRate <= MaxReportRateAllowed;

    private static int Percent(decimal value, decimal required)
        => required <= 0 ? 100 : Math.Clamp((int)Math.Round(value / required * 100), 0, 100);
}

public sealed class RecentTourVm
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? OfferingStatus { get; init; }
    public bool OffersPrivateTour { get; init; }
    public DateTime? AssignedAt { get; init; }
}
