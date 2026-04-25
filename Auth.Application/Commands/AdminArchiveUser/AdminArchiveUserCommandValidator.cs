using FluentValidation;

namespace Auth.Application.Commands.AdminArchiveUser;

public sealed class AdminArchiveUserCommandValidator : AbstractValidator<AdminArchiveUserCommand>
{
    public AdminArchiveUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}
