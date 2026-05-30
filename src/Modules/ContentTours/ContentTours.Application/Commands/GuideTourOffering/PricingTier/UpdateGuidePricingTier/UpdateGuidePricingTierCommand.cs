using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.UpdateGuidePricingTier;

public sealed record UpdateGuidePricingTierCommand(
    Guid TierId,
    string Name,
    decimal Price,
    string Currency,
    int MinParticipants,
    int MaxParticipants,
    string? Description) : ICommand;
