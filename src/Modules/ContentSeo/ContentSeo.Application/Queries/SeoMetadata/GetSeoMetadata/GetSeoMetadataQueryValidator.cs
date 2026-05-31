using FluentValidation;

namespace ContentSeo.Application.Queries.SeoMetadata.GetSeoMetadata;

public sealed class GetSeoMetadataQueryValidator : AbstractValidator<GetSeoMetadataQuery>
{
    public GetSeoMetadataQueryValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
    }
}
