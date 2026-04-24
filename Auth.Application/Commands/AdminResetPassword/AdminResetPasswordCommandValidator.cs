using FluentValidation;

namespace Auth.Application.Commands.AdminResetPassword;

/// <summary>
/// Input validation for <see cref="AdminResetPasswordCommand"/>.
/// </summary>
public sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEqual(Guid.Empty)
            .WithMessage("TargetUserId is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
