using FluentValidation;

namespace ContentCore.Application.Queries.Attachment.GetEntityAttachments;

public sealed class GetEntityAttachmentsQueryValidator : AbstractValidator<GetEntityAttachmentsQuery>
{
    public GetEntityAttachmentsQueryValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
    }
}
