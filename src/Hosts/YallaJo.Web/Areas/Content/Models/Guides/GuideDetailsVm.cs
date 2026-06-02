namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class GuideDetailsVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public int YearsOfExperience { get; init; }
    public bool HasFirstAid { get; init; }
    public string? MoTALicenseNumber { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int TourCount { get; init; }

    public string GuideImageUrl { get; init; } = string.Empty;
    public string GuideImageAlt { get; init; } = string.Empty;
    public bool HasGuideImage { get; init; }

    public IReadOnlyList<SpecializationVm> Specializations { get; init; } = [];
    public IReadOnlyList<GuideTourVm> Tours { get; init; } = [];

    public bool HasTours => Tours.Count > 0;
    public bool HasSpecializations => Specializations.Count > 0;
    public bool HasLicense => !string.IsNullOrWhiteSpace(MoTALicenseNumber);
}
