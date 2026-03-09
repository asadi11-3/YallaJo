using FluentValidation;

namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.UploadedByUserId)
            .NotEmpty();

        RuleFor(x => x.EntityType)
            .IsInEnum();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}