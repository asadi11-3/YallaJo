using FluentValidation;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.UpdateGuidePricingTier;

public sealed class UpdateGuidePricingTierCommandValidator : AbstractValidator<UpdateGuidePricingTierCommand>
{
    public UpdateGuidePricingTierCommandValidator()
    {
        RuleFor(x => x.TierId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.MinParticipants).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MaxParticipants).GreaterThanOrEqualTo(x => x.MinParticipants);
    }
}
