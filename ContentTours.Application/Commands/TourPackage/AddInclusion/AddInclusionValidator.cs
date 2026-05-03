using FluentValidation;

namespace ContentTours.Application.Commands.TourPackage.AddInclusion;

public sealed class AddInclusionValidator : AbstractValidator<AddInclusionCommand>
{
    public AddInclusionValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
