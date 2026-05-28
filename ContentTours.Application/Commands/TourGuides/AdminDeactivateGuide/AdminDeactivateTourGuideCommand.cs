using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.AdminDeactivateGuide;

public sealed record AdminDeactivateTourGuideCommand(Guid TourGuideId) : ICommand;
