namespace YallaJo.Web.Areas.Public.Models.Guides;

public sealed class GuideListItemResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? Slug { get; init; }
    public string? AvatarUrl { get; init; }
    public string Bio { get; init; } = "";
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public List<GuideLanguageResponse> Languages { get; init; } = [];
    public List<GuideSpecializationResponse> Specializations { get; init; } = [];
}

public sealed class GuideLanguageResponse
{
    public Guid LanguageId { get; init; }
    public string Proficiency { get; init; } = "";
}

public sealed class GuideSpecializationResponse
{
    public Guid SpecializationId { get; init; }
}

public sealed class PaginatedGuidesResponse
{
    public List<GuideListItemResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class GuideDetailResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string Bio { get; init; } = "";
    public int YearsOfExperience { get; init; }
    public bool HasFirstAid { get; init; }
    public string? MoTALicenseNumber { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }
    public List<GuideLanguageResponse> Languages { get; init; } = [];
    public List<GuideSpecializationResponse> Specializations { get; init; } = [];
}

public sealed class GuideTourItemResponse
{
    public Guid TourId { get; init; }
    public string Title { get; init; } = "";
    public string? Slug { get; init; }
    public bool IsProposer { get; init; }
    public string OfferingStatus { get; init; } = "";
    public bool OffersPrivateTour { get; init; }
    public DateTime? AssignedAt { get; init; }
}

public sealed class GuideToursResponse
{
    public List<GuideTourItemResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
