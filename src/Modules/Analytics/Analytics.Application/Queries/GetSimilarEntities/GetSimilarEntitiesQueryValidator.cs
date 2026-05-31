using FluentValidation;

namespace Analytics.Application.Queries.GetSimilarEntities;

public sealed class GetSimilarEntitiesQueryValidator : AbstractValidator<GetSimilarEntitiesQuery>
{
    public GetSimilarEntitiesQueryValidator()
    {
        RuleFor(x => x.SourceKind).IsInEnum();
    }
}
