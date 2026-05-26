using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.AdminUpdateGuide;

public sealed record AdminUpdateTourGuideCommand(
    Guid TourGuideId,
    string? Bio,
    int? YearsOfExperience,
    bool? HasFirstAid,
    string? MoTALicenseNumber) : ICommand;
