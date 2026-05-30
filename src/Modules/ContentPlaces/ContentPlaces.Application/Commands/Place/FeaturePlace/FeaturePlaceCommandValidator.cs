using FluentValidation;

namespace ContentPlaces.Application.Commands.Place.FeaturePlace;

public sealed class FeaturePlaceCommandValidator : AbstractValidator<FeaturePlaceCommand>
{
    public FeaturePlaceCommandValidator()
    {
        RuleFor(x => x.PlaceId).NotEmpty();
    }
}
