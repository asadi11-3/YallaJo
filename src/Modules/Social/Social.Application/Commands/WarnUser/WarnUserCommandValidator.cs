using FluentValidation;

namespace Social.Application.Commands.WarnUser;

public sealed class WarnUserCommandValidator : AbstractValidator<WarnUserCommand>
{
    public WarnUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
