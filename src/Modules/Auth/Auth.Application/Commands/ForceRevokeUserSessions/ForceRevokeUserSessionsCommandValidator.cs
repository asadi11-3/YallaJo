using FluentValidation;

namespace Auth.Application.Commands.ForceRevokeUserSessions;

public sealed class ForceRevokeUserSessionsCommandValidator : AbstractValidator<ForceRevokeUserSessionsCommand>
{
    public ForceRevokeUserSessionsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}
