using FluentValidation;

namespace Booking.Application.Commands.ResolveBookingDispute;

public sealed class ResolveBookingDisputeCommandValidator : AbstractValidator<ResolveBookingDisputeCommand>
{
    public ResolveBookingDisputeCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.ResolutionNotes)
            .NotEmpty()
            .MinimumLength(10).WithMessage("Resolution notes must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Resolution notes must be at most 2000 characters.");
    }
}
