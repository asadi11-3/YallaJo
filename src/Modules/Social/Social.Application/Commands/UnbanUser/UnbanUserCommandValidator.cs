using FluentValidation;

namespace Social.Application.Commands.UnbanUser;

public sealed class UnbanUserCommandValidator : AbstractValidator<UnbanUserCommand>
{
    public UnbanUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
