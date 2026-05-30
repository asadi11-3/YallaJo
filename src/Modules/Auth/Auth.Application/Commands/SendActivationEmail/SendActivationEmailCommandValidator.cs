using FluentValidation;

namespace Auth.Application.Commands.SendActivationEmail;

public sealed class SendActivationEmailCommandValidator : AbstractValidator<SendActivationEmailCommand>
{
    public SendActivationEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
