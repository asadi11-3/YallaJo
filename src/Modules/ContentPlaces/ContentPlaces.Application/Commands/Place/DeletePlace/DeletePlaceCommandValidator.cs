using FluentValidation;

namespace ContentPlaces.Application.Commands.Place.DeletePlace;

public sealed class DeletePlaceCommandValidator : AbstractValidator<DeletePlaceCommand>
{
    public DeletePlaceCommandValidator()
    {
        RuleFor(x => x.PlaceId).NotEmpty();
    }
}
