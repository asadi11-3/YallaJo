using FluentValidation;

namespace Booking.Application.Commands.CompleteTourBooking;

public sealed class CompleteTourBookingCommandValidator : AbstractValidator<CompleteTourBookingCommand>
{
    public CompleteTourBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
