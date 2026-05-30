using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.Unassign;

public sealed record UnassignTourGuideCommand(
    Guid TourId,
    Guid TourGuideUserId
) : ICommand;
