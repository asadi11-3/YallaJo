// Auth.Application/Commands/RevokeSession/RevokeSessionCommandValidator.cs
using FluentValidation;

namespace Auth.Application.Commands.RevokeSession;

public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty();
    }
}
