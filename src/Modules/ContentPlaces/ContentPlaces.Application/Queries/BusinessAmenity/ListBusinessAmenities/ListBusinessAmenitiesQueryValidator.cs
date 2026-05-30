using FluentValidation;

namespace ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;

public sealed class ListBusinessAmenitiesQueryValidator
    : AbstractValidator<ListBusinessAmenitiesQuery>
{
    public ListBusinessAmenitiesQueryValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
