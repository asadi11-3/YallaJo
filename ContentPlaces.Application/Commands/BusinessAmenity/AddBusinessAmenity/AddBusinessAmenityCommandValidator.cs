using FluentValidation;

namespace ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;

public sealed class AddBusinessAmenityCommandValidator
    : AbstractValidator<AddBusinessAmenityCommand>
{
    public AddBusinessAmenityCommandValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty() // not empty
            .MaximumLength(200) // max length
            .Matches(@"^[\p{L}0-9 ]+$") // supports all languages
            .Must(name => !string.IsNullOrWhiteSpace(name)); // prevent only spaces

        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
