using FluentValidation;

namespace ContentSeo.Application.Queries.FaqItem.GetFaqItems;

public sealed class GetFaqItemsQueryValidator : AbstractValidator<GetFaqItemsQuery>
{
    public GetFaqItemsQueryValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
    }
}
