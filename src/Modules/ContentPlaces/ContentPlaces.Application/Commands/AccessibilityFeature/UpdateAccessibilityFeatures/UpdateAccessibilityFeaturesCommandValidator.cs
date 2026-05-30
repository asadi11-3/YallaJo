using FluentValidation;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;

public sealed class UpdateAccessibilityFeaturesCommandValidator
    : AbstractValidator<UpdateAccessibilityFeaturesCommand>
{
    public UpdateAccessibilityFeaturesCommandValidator()
    {
        // PlaceId
        RuleFor(x => x.PlaceId)
            .NotEmpty();

        // Features list
        RuleFor(x => x.Features)
            .NotEmpty();

        RuleForEach(x => x.Features).ChildRules(feature =>
        {
            feature.RuleFor(x => x.FeatureType)
                .IsInEnum();

            feature.RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);
        });
    }
}
