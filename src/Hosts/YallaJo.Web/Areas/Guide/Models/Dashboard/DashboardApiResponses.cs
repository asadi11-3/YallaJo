namespace YallaJo.Web.Areas.Guide.Models.Dashboard;

public sealed class TourGuideProfileResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string Bio { get; init; } = string.Empty;
    public int YearsOfExperience { get; init; }
    public bool HasFirstAid { get; init; }
    public string? MoTALicenseNumber { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public IReadOnlyList<TourGuideLanguageResponse> Languages { get; init; } = [];
    public IReadOnlyList<TourGuideSpecializationResponse> Specializations { get; init; } = [];
}

public sealed class TourGuideLanguageResponse
{
    public Guid LanguageId { get; init; }
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Proficiency { get; init; }
}

public sealed class TourGuideSpecializationResponse
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Icon { get; init; }
}

public sealed class GuideEarningsSummaryResponse
{
    public decimal TotalEarned { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal PendingPayout { get; init; }
    public decimal CommissionDeducted { get; init; }
    public decimal NetEarnings { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public sealed class GuideToursResponse
{
    public IReadOnlyList<GuideTourItemResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed class GuideTourItemResponse
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public bool IsProposer { get; init; }
    public string? OfferingStatus { get; init; }
    public bool OffersPrivateTour { get; init; }
    public DateTime? AssignedAt { get; init; }
}

public sealed class GuideAvailabilityBlockResponse
{
    public Guid Id { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
}
