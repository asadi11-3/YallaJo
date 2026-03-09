using FluentValidation;

namespace ContentCore.Application.Commands.Attachment.ReorderAttachments;

public sealed class ReorderAttachmentsCommandValidator : AbstractValidator<ReorderAttachmentsCommand>
{
    public ReorderAttachmentsCommandValidator()
    {
        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.EntityType)
            .IsInEnum();

        RuleFor(x => x.OrderedAttachmentIds)
            .NotEmpty()
            .WithMessage("At least one attachment ID is required.");
    }
}