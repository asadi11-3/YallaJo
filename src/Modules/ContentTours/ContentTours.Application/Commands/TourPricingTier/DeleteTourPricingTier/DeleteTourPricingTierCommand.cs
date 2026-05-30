using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPricingTier.DeleteTourPricingTier;

public sealed record DeleteTourPricingTierCommand(Guid TourId, Guid TierId) : ICommand;
