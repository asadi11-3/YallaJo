using FluentValidation;

namespace Analytics.Application.Queries.GetEntitySuggestions;

public sealed class GetEntitySuggestionsQueryValidator : AbstractValidator<GetEntitySuggestionsQuery>
{
    public GetEntitySuggestionsQueryValidator()
    {
        RuleFor(x => x.SourceKind).IsInEnum();
    }
}
