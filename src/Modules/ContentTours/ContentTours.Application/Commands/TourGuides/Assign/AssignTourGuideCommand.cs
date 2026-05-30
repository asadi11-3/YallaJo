using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.Assign;

public sealed record AssignTourGuideCommand(
    Guid TourId,
    Guid TourGuideUserId,
    bool IsPrimary
) : ICommand;
