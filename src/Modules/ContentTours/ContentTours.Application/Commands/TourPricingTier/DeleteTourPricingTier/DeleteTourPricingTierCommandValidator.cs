using FluentValidation;

namespace ContentTours.Application.Commands.TourPricingTier.DeleteTourPricingTier;

public sealed class DeleteTourPricingTierCommandValidator : AbstractValidator<DeleteTourPricingTierCommand>
{
    public DeleteTourPricingTierCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
        RuleFor(x => x.TierId).NotEqual(Guid.Empty);
    }
}
