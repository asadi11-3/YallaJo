using FluentValidation;

namespace Auth.Application.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
