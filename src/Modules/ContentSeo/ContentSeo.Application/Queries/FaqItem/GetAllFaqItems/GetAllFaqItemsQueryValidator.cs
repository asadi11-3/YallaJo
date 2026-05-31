using FluentValidation;

namespace ContentSeo.Application.Queries.FaqItem.GetAllFaqItems;

public sealed class GetAllFaqItemsQueryValidator : AbstractValidator<GetAllFaqItemsQuery>
{
    public GetAllFaqItemsQueryValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum().When(x => x.EntityType.HasValue);
    }
}
