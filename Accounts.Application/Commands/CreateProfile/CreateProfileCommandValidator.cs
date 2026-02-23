using FluentValidation;

namespace Accounts.Application.Commands.CreateProfile;

public sealed class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).MaximumLength(200);
        RuleFor(x => x.AvatarUrl).MaximumLength(2048);
    }
}
