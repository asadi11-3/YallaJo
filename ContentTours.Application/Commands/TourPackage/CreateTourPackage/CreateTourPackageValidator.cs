using FluentValidation;

namespace ContentTours.Application.Commands.TourPackage.CreateTourPackage;

public sealed class CreateTourPackageValidator : AbstractValidator<CreateTourPackageCommand>
{
    public CreateTourPackageValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a valid 3-letter ISO code");

        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .When(x => x.ValidTo.HasValue && x.ValidFrom.HasValue)
            .WithMessage("ValidTo must be after ValidFrom");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0)
            .When(x => x.MaxParticipants.HasValue);
    }
}
