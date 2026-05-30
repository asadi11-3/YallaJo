using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.EnablePrivateTour;

public sealed record EnablePrivateTourCommand(
    Guid TourId,
    Guid TourGuideId,
    decimal? Multiplier,
    decimal? FlatPrice) : ICommand;
