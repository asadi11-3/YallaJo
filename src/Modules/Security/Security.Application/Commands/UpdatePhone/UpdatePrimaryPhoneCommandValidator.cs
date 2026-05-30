using FluentValidation;

namespace Security.Application.Commands.UpdatePhone;

public sealed class UpdatePrimaryPhoneCommandValidator : AbstractValidator<UpdatePrimaryPhoneCommand>
{
    public UpdatePrimaryPhoneCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-()]{7,20}$")
            .WithMessage("Invalid phone number format.");
    }
}
