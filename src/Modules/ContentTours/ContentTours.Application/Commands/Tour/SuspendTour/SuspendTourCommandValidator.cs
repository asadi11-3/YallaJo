using FluentValidation;

namespace ContentTours.Application.Commands.Tour.SuspendTour;

public sealed class SuspendTourCommandValidator : AbstractValidator<SuspendTourCommand>
{
    public SuspendTourCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0)
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A suspension reason is required.")
            .MaximumLength(1000);
    }
}
