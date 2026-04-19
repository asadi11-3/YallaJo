using FluentValidation;

namespace Auth.Application.Commands.ResendInvite;

public sealed class ResendInviteCommandValidator : AbstractValidator<ResendInviteCommand>
{
    public ResendInviteCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
