using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.SuspendGuideOffering;

public sealed record SuspendGuideOfferingCommand(
    Guid TourId,
    Guid TourGuideId,
    string Reason) : ICommand;
