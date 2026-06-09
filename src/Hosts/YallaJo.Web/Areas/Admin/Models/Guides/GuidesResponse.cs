namespace YallaJo.Web.Areas.Admin.Models.Guides;

public sealed class TourGuideProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Bio { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public bool HasFirstAid { get; set; }
    public string? MoTALicenseNumber { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int TourCount { get; set; }
    public IReadOnlyList<TourGuideLanguageResponse> Languages { get; set; } = [];
    public IReadOnlyList<TourGuideSpecializationResponse> Specializations { get; set; } = [];
}

public sealed class TourGuideLanguageResponse
{
    public Guid LanguageId { get; set; }
    public string? Name { get; set; }
}

public sealed class TourGuideSpecializationResponse
{
    public Guid SpecializationId { get; set; }
    public string? Name { get; set; }
}

// Minimal projection of GET /api/v1/guides (public list of active guides) — used to
// populate the guide-name picker so admins never type a raw GUID (F10).
public sealed class AdminGuideListResponse
{
    public IReadOnlyList<AdminGuideListItemResponse> Items { get; set; } = [];
}

public sealed class AdminGuideListItemResponse
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
}
