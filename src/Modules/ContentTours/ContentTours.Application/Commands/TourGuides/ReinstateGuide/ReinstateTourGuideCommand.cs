using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourGuides.ReinstateGuide;

public sealed record ReinstateTourGuideCommand(Guid TourGuideId) : ICommand;
