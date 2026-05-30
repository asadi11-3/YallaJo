using FluentValidation;

namespace ContentTours.Application.Commands.GuideTourOffering.PricingTier.CreateGuidePricingTier;

public sealed class CreateGuidePricingTierCommandValidator : AbstractValidator<CreateGuidePricingTierCommand>
{
    public CreateGuidePricingTierCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.TourGuideId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.MinParticipants).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MaxParticipants).GreaterThanOrEqualTo(x => x.MinParticipants);
    }
}
