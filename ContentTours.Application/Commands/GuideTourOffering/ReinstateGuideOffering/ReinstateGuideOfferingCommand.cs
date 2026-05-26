using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.ReinstateGuideOffering;

public sealed record ReinstateGuideOfferingCommand(Guid TourId, Guid TourGuideId) : ICommand;
