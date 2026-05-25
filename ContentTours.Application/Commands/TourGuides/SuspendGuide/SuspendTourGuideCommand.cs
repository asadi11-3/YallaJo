using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourGuides.SuspendGuide;

public sealed record SuspendTourGuideCommand(Guid TourGuideId, string Reason) : ICommand;
