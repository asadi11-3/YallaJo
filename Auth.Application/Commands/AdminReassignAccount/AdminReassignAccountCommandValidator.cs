using FluentValidation;

namespace Auth.Application.Commands.AdminReassignAccount;

/// <summary>
/// Input validation for <see cref="AdminReassignAccountCommand"/>.
/// Matches the shape of the other admin lifecycle commands: non-empty
/// target id, valid email with max length, bounded reason.
/// </summary>
public sealed class AdminReassignAccountCommandValidator : AbstractValidator<AdminReassignAccountCommand>
{
    public AdminReassignAccountCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEqual(Guid.Empty)
            .WithMessage("TargetUserId is required.");

        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
