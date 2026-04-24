using FluentValidation;

namespace Auth.Application.Commands.SendActivationEmail;

/// <summary>
/// Input validation for <see cref="SendActivationEmailCommand"/>.
/// </summary>
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
