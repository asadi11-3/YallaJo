using FluentValidation;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateBusinessAccessibilityFeatures;

public sealed class UpdateBusinessAccessibilityFeaturesCommandValidator
    : AbstractValidator<UpdateBusinessAccessibilityFeaturesCommand>
{
    public UpdateBusinessAccessibilityFeaturesCommandValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.Features).NotNull();
    }
}
