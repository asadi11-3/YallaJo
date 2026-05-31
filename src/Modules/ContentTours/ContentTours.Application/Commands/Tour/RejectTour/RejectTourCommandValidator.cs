using FluentValidation;

namespace ContentTours.Application.Commands.Tour.RejectTour;

public sealed class RejectTourCommandValidator : AbstractValidator<RejectTourCommand>
{
    public RejectTourCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A rejection reason is required.")
            .MaximumLength(1000);
    }
}
