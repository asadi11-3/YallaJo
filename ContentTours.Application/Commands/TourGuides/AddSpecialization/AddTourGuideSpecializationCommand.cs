using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.AddSpecialization;

public sealed record AddTourGuideSpecializationCommand(
    Guid TourGuideId,
    Guid CallerUserId,
    Guid SpecializationId) : ICommand;
