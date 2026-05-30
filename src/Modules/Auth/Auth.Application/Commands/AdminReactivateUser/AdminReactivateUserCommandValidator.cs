using FluentValidation;

namespace Auth.Application.Commands.AdminReactivateUser;

public sealed class AdminReactivateUserCommandValidator : AbstractValidator<AdminReactivateUserCommand>
{
    public AdminReactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}
