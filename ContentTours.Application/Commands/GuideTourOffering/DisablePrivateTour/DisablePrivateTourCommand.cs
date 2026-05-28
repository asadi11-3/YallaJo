using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.DisablePrivateTour;

public sealed record DisablePrivateTourCommand(Guid TourId, Guid TourGuideId) : ICommand;
