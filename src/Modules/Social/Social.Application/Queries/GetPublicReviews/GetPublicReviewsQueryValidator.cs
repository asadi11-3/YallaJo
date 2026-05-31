using FluentValidation;

namespace Social.Application.Queries.GetPublicReviews;

public sealed class GetPublicReviewsQueryValidator : AbstractValidator<GetPublicReviewsQuery>
{
    public GetPublicReviewsQueryValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
    }
}
