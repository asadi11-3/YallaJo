using FluentValidation;

namespace Booking.Application.Commands.RejectTourBooking;

public sealed class RejectTourBookingCommandValidator : AbstractValidator<RejectTourBookingCommand>
{
    public RejectTourBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10).WithMessage("Rejection reason must be at least 10 characters.")
            .MaximumLength(500);
    }
}
