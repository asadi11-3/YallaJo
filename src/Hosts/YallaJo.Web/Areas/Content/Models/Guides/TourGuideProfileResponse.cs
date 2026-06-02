namespace YallaJo.Web.Areas.Content.Models.Guides;

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
    public List<TourGuideLanguageResponse> Languages { get; init; } = [];
    public List<TourGuideSpecializationResponse> Specializations { get; init; } = [];
}

/// <summary>Mirrors <c>TourGuideLanguageDto</c>.</summary>
public sealed class TourGuideLanguageResponse
{
    public Guid LanguageId { get; init; }
    public string? Proficiency { get; init; }
}
