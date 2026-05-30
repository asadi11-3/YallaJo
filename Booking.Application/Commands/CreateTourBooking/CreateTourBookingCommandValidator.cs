using FluentValidation;

namespace Booking.Application.Commands.CreateTourBooking;

/// <summary>
/// Input-shape validation only. Cross-aggregate / DB checks live in the handler.
/// </summary>
public sealed class CreateTourBookingCommandValidator : AbstractValidator<CreateTourBookingCommand>
{
    private const int MaxParticipantCount = 100;
    private const int MinAdultCount = 1;

    public CreateTourBookingCommandValidator()
    {
        RuleFor(x => x.TourId)
            .NotEmpty().WithMessage("TourId is required.");

        // F21 2026-05-29: GuideId was required by handler but not by validator;
        // missing/empty GuideId fell through to handler and returned 400 with
        // misleading code `TourBooking.InvalidState`. Catching here surfaces a
        // proper FluentValidation 400 with field-level errors per RFC 7807.
        RuleFor(x => x.GuideId)
            .NotEmpty().WithMessage("GuideId is required.");

        RuleFor(x => x.AvailabilitySlotId)
            .NotEmpty().WithMessage("AvailabilitySlotId is required.");

        RuleFor(x => x.ParticipantBreakdown)
            .NotNull().WithMessage("ParticipantBreakdown is required.");

        When(x => x.ParticipantBreakdown is not null, () =>
        {
            RuleFor(x => x.ParticipantBreakdown.Adult)
                .GreaterThanOrEqualTo(MinAdultCount).WithMessage("At least one adult participant is required.");

            RuleFor(x => x.ParticipantBreakdown.Child)
                .GreaterThanOrEqualTo(0).WithMessage("Child count cannot be negative.");

            RuleFor(x => x.ParticipantBreakdown.Infant)
                .GreaterThanOrEqualTo(0).WithMessage("Infant count cannot be negative.");

            RuleFor(x => x.ParticipantBreakdown.Senior)
                .GreaterThanOrEqualTo(0).WithMessage("Senior count cannot be negative.");

            RuleFor(x => x.ParticipantBreakdown.Total)
                .LessThanOrEqualTo(MaxParticipantCount).WithMessage($"Total participants cannot exceed {MaxParticipantCount}.");
        });

        RuleFor(x => x.PromoCode)
            .MaximumLength(20).WithMessage("PromoCode cannot exceed 20 characters.")
            .Matches("^[A-Z0-9]+$").WithMessage("PromoCode must contain only uppercase letters and digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.PromoCode));

        RuleFor(x => x.LoyaltyPointsToRedeem)
            .GreaterThanOrEqualTo(0).WithMessage("LoyaltyPointsToRedeem cannot be negative.");

        RuleFor(x => x.SpecialRequests)
            .MaximumLength(2000).WithMessage("SpecialRequests cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.SpecialRequests));
    }
}
