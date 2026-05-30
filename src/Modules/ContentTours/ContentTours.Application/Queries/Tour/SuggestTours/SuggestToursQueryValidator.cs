using FluentValidation;

namespace ContentTours.Application.Queries.Tour.SuggestTours;

public sealed class SuggestToursQueryValidator : AbstractValidator<SuggestToursQuery>
{
    public SuggestToursQueryValidator()
    {
        RuleFor(x => x.Q).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}
