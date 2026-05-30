using FluentValidation;

namespace ContentTours.Application.Queries.Tour.SearchTours;

public sealed class SearchToursQueryValidator : AbstractValidator<SearchToursQuery>
{
    public SearchToursQueryValidator()
    {
        RuleFor(x => x.Request.Q).MaximumLength(200).When(x => x.Request.Q is not null);
        RuleFor(x => x.Request.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Request.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Request.Sort).IsInEnum();
    }
}
