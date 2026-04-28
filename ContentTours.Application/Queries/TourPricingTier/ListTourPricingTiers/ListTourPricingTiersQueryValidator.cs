using FluentValidation;

namespace ContentTours.Application.Queries.TourPricingTier.ListTourPricingTiers;

public sealed class ListTourPricingTiersQueryValidator : AbstractValidator<ListTourPricingTiersQuery>
{
    public ListTourPricingTiersQueryValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
    }
}
