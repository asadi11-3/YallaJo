using FluentValidation;

namespace Auth.Application.Commands.AdminSuspendUser;

public sealed class AdminSuspendUserCommandValidator : AbstractValidator<AdminSuspendUserCommand>
{
    public AdminSuspendUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}
