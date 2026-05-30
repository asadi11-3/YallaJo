using FluentValidation;

namespace ContentTours.Application.Queries.Tour.ListMyTours;

public sealed class ListMyToursQueryValidator : AbstractValidator<ListMyToursQuery>
{
    public ListMyToursQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
