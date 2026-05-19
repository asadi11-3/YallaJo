using FluentValidation;

namespace Booking.Application.Commands.ConfirmTourBooking;

public sealed class ConfirmTourBookingCommandValidator : AbstractValidator<ConfirmTourBookingCommand>
{
    public ConfirmTourBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
