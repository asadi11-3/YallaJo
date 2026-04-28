using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPricingTier.CreateTourPricingTier;

public sealed record CreateTourPricingTierCommand(
    Guid TourId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    ParticipantType ParticipantType,
    int MinParticipants,
    int? MaxParticipants
) : ICommand<CreateTourPricingTierResult>;
