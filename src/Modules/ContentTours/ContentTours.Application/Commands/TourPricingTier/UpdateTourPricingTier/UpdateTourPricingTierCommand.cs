using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPricingTier.UpdateTourPricingTier;

public sealed record UpdateTourPricingTierCommand(
    Guid TourId,
    Guid TierId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    ParticipantType ParticipantType,
    int MinParticipants,
    int? MaxParticipants,
    bool IsActive
) : ICommand;
