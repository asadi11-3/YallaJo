namespace ContentTours.Application.Queries.TourGuides.Common;

public sealed record TourGuideProfileDto(
    Guid Id,
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string Bio,
    int YearsOfExperience,
    bool HasFirstAid,
    string? MoTALicenseNumber,
    decimal AverageRating,
    int ReviewCount,
    int TourCount,
    IReadOnlyList<TourGuideLanguageDto> Languages,
    IReadOnlyList<TourGuideSpecializationDto> Specializations);
