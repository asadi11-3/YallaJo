using FluentValidation;

namespace ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;

public sealed class RemoveBusinessAmenityCommandValidator
    : AbstractValidator<RemoveBusinessAmenityCommand>
{
    public RemoveBusinessAmenityCommandValidator()
    {
        RuleFor(x => x.AmenityId).NotEmpty();
    }
}
