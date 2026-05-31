using FluentValidation;

namespace Social.Application.Queries.GetRatingSummary;

public sealed class GetRatingSummaryQueryValidator : AbstractValidator<GetRatingSummaryQuery>
{
    public GetRatingSummaryQueryValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
    }
}
