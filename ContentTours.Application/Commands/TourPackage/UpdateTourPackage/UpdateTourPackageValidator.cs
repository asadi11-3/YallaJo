using FluentValidation;

namespace ContentTours.Application.Commands.TourPackage.UpdateTourPackage;

public sealed class UpdateTourPackageValidator : AbstractValidator<UpdateTourPackageCommand>
{
    public UpdateTourPackageValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a valid 3-letter ISO code");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0)
            .When(x => x.MaxParticipants.HasValue);
    }
}
