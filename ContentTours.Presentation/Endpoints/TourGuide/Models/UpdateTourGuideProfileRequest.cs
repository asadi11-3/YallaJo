namespace ContentTours.Presentation.Endpoints.TourGuide.Models;

public sealed record UpdateTourGuideProfileRequest(
    string Bio,
    int YearsOfExperience,
    bool HasFirstAid,
    string? MoTALicenseNumber);
