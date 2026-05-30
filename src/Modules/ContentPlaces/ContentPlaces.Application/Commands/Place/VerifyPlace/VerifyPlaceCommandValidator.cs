using FluentValidation;

namespace ContentPlaces.Application.Commands.Place.VerifyPlace;

public sealed class VerifyPlaceCommandValidator : AbstractValidator<VerifyPlaceCommand>
{
    public VerifyPlaceCommandValidator()
    {
        RuleFor(x => x.PlaceId).NotEmpty();
    }
}
