using ContentBlogs.Domain.Enums;
using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.SendInvitation;

public sealed class SendCreatorInvitationCommandValidator
    : AbstractValidator<SendCreatorInvitationCommand>
{
    public SendCreatorInvitationCommandValidator()
    {
        RuleFor(x => x.Email!)
            .NotEmpty().WithMessage("Email is required for email invitations.")
            .EmailAddress()
            .MaximumLength(320)
            .When(x => x.Kind == CreatorInvitationKind.Email);

        RuleFor(x => x.InvitedUserId!.Value)
            .NotEqual(Guid.Empty).WithMessage("InvitedUserId is required for in-app invitations.")
            .When(x => x.Kind == CreatorInvitationKind.InApp);
    }
}
