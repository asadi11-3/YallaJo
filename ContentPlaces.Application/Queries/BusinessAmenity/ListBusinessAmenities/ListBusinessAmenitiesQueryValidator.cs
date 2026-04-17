using FluentValidation;

namespace ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;

public sealed class ListBusinessAmenitiesQueryValidator
    : AbstractValidator<ListBusinessAmenitiesQuery>
{
    public ListBusinessAmenitiesQueryValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
    }
}
