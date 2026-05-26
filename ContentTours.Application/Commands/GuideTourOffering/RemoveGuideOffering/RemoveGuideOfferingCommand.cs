using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.RemoveGuideOffering;

public sealed record RemoveGuideOfferingCommand(Guid TourId, Guid TourGuideId) : ICommand;
