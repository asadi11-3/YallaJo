using Accounts.Domain.Enums;
using FluentValidation;

namespace Accounts.Application.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob!.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .When(x => x.DateOfBirth is not null);

        RuleFor(x => x.DateOfBirth)
            .Must(dob => DateOnly.FromDateTime(DateTime.UtcNow).Year - dob!.Value.Year >= 13)
            .WithMessage("You must be at least 13 years old.")
            .When(x => x.DateOfBirth is not null);

        RuleFor(x => x.Gender)
            .IsInEnum()
            .WithMessage("Gender must be Male, Female, or Other.")
            .When(x => x.Gender is not null);

        RuleFor(x => x.Country).MaximumLength(100).When(x => x.Country is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.AddressLine).MaximumLength(300).When(x => x.AddressLine is not null);
    }
}
