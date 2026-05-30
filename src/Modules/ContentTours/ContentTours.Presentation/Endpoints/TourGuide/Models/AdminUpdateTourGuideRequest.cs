namespace ContentTours.Presentation.Endpoints.TourGuide.Models;

public sealed record AdminUpdateTourGuideRequest(
    string? Bio,
    int? YearsOfExperience,
    bool? HasFirstAid,
    string? MoTALicenseNumber);
