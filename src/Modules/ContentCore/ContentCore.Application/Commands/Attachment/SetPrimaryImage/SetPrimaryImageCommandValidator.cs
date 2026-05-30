using FluentValidation;

namespace ContentCore.Application.Commands.Attachment.SetPrimaryImage;

public sealed class SetPrimaryImageCommandValidator : AbstractValidator<SetPrimaryImageCommand>
{
    public SetPrimaryImageCommandValidator()
    {
        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.AttachmentId)
            .NotEmpty();

        RuleFor(x => x.EntityType)
            .IsInEnum();
    }
}