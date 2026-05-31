using FluentValidation;

namespace ContentCore.Application.Queries.Translation.GetEntityTranslations;

public sealed class GetEntityTranslationsQueryValidator : AbstractValidator<GetEntityTranslationsQuery>
{
    public GetEntityTranslationsQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
