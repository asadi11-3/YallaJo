using FluentValidation;

namespace Booking.Application.Commands.OpenBookingDispute;

public sealed class OpenBookingDisputeCommandValidator : AbstractValidator<OpenBookingDisputeCommand>
{
    public OpenBookingDisputeCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10).WithMessage("Dispute reason must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Dispute reason must be at most 2000 characters.");
    }
}
