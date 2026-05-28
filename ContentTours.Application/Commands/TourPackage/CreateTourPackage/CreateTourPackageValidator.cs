using FluentValidation;

namespace ContentTours.Application.Commands.TourPackage.CreateTourPackage;

/// <summary>
/// Surface-level validation for <see cref="CreateTourPackageCommand"/>. PDF rules
/// that require domain knowledge (≥2 distinct, ownership, currency match, capacity)
/// are enforced in the handler so they map to dedicated error codes.
/// </summary>
public sealed class CreateTourPackageValidator : AbstractValidator<CreateTourPackageCommand>
{
    public CreateTourPackageValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a valid 3-letter ISO code.");

        RuleFor(x => x.MaxParticipants!.Value)
            .GreaterThanOrEqualTo(1)
            .When(x => x.MaxParticipants.HasValue);

        RuleFor(x => x.ValidFrom!.Value)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ValidFrom.HasValue)
            .WithMessage("ValidFrom must be in the future.");

        RuleFor(x => x)
            .Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidTo.Value > x.ValidFrom.Value)
            .WithMessage("ValidTo must be greater than ValidFrom.");

        RuleFor(x => x.IncludedTourIds)
            .NotNull()
            .WithMessage("IncludedTourIds is required.")
            .Must(ids => ids != null && ids.Where(id => id != Guid.Empty).Distinct().Count() >= 2)
            .WithMessage("Provide at least 2 distinct, non-empty IncludedTourIds.")
            .Must(ids => ids == null || ids.Where(id => id != Guid.Empty).Distinct().Count() <= 10)
            .WithMessage("A package cannot include more than 10 tours.");

        RuleFor(x => x.Inclusions)
            .NotNull()
            .WithMessage("Inclusions is required.");

        RuleForEach(x => x.Inclusions)
            .NotEmpty()
            .MaximumLength(500)
            .When(x => x.Inclusions is not null);
    }
}
