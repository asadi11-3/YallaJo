using FluentValidation;

namespace ContentTours.Application.Commands.TourPackage.RejectTourPackage;

public sealed class RejectTourPackageCommandValidator : AbstractValidator<RejectTourPackageCommand>
{
    public RejectTourPackageCommandValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty()
            .WithMessage("PackageId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A rejection reason is required.")
            .MaximumLength(2000)
            .WithMessage("Rejection reason must be at most 2000 characters.");
    }
}
