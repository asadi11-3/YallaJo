using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.RemoveSpecialization;

public sealed record RemoveTourGuideSpecializationCommand(
    Guid TourGuideId,
    Guid SpecializationId) : ICommand;
