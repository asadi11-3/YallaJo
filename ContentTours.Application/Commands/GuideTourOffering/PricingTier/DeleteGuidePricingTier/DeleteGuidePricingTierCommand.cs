using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.DeleteGuidePricingTier;

public sealed record DeleteGuidePricingTierCommand(Guid TierId) : ICommand;
