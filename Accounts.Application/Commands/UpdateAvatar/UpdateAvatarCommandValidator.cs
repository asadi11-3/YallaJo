using FluentValidation;

namespace Accounts.Application.Commands.UpdateAvatar;

public sealed class UpdateAvatarCommandValidator : AbstractValidator<UpdateAvatarCommand>
{
    public UpdateAvatarCommandValidator()
    {
        RuleFor(x => x.AvatarUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("AvatarUrl must be a valid URL.");
    }
}
