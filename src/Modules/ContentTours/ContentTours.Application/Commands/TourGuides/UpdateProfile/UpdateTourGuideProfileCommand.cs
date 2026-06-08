using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.UpdateProfile;

public sealed record UpdateTourGuideProfileCommand(
    Guid TourGuideId,
    string Bio,
    int YearsOfExperience,
    bool HasFirstAid,
    string? MoTALicenseNumber) : ICommand;
