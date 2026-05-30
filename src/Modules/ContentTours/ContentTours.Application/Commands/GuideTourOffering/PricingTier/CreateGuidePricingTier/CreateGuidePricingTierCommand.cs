using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.CreateGuidePricingTier;

public sealed record CreateGuidePricingTierCommand(
    Guid TourId,
    Guid TourGuideId,
    string Name,
    decimal Price,
    string Currency,
    int MinParticipants,
    int MaxParticipants,
    string? Description) : ICommand<Guid>;
