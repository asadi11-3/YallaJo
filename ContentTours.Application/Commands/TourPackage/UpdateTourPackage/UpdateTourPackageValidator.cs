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

        // PDF: Price >= 0 (free packages are allowed).
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

        RuleFor(x => x.IncludedTourIds)
            .NotNull()
            .WithMessage("IncludedTourIds is required.")
            .Must(ids => ids != null && ids.Where(id => id != Guid.Empty).Distinct().Count() >= 2)
            .WithMessage("Provide at least 2 distinct, non-empty IncludedTourIds.");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
