using FluentValidation;

namespace ContentCore.Application.Commands.Attachment.DeleteAttachment;

public sealed class DeleteAttachmentCommandValidator : AbstractValidator<DeleteAttachmentCommand>
{
    public DeleteAttachmentCommandValidator()
    {
        RuleFor(x => x.AttachmentId)
            .NotEmpty();
    }
}