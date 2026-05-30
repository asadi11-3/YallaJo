using FluentValidation;

namespace Booking.Application.Commands.AdminForceRefund;

public sealed class AdminForceRefundCommandValidator : AbstractValidator<AdminForceRefundCommand>
{
    public AdminForceRefundCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("BookingId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for force refund (audit trail).")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
