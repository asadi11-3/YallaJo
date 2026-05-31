using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.CreateBusiness;

public sealed class CreateBusinessCommandValidator : AbstractValidator<CreateBusinessCommand>
{
    public CreateBusinessCommandValidator()
    {
        RuleFor(x => x.BusinessType).IsInEnum();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Slug)
            .MaximumLength(300)
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.PlaceId)
            .NotEmpty();

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m);

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Website)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.LicenseNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.LicenseNumber));

        RuleFor(x => x.TaxId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.TaxId));
    }
}
