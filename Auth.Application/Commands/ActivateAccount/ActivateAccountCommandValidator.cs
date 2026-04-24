using FluentValidation;

namespace Auth.Application.Commands.ActivateAccount;

/// <summary>
/// Input validation for <see cref="ActivateAccountCommand"/>. Mirrors the
/// legacy <c>AcceptInviteCommandValidator</c>.
/// </summary>
public sealed class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Token)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }
}
