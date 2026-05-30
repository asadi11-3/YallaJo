using FluentValidation;

namespace Booking.Application.Commands.CancelTourBooking;

internal sealed class CancelTourBookingCommandValidator : AbstractValidator<CancelTourBookingCommand>
{
    public CancelTourBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();

        // Provider/admin reason is required (>=10 chars) — that is enforced inside the handler
        // because the source is determined at runtime. Here we just enforce overall length cap.
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
