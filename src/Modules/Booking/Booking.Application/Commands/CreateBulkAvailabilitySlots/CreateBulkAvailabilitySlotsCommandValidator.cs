using FluentValidation;

namespace Booking.Application.Commands.CreateBulkAvailabilitySlots;

public sealed class CreateBulkAvailabilitySlotsCommandValidator : AbstractValidator<CreateBulkAvailabilitySlotsCommand>
{
    public CreateBulkAvailabilitySlotsCommandValidator()
    {
        RuleFor(x => x.Recurrence).IsInEnum();

        RuleFor(x => x.TourId)
            .NotEmpty().WithMessage("TourId is required.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("StartDate must be less than or equal to EndDate.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be strictly later than StartTime.");

        RuleFor(x => x.MaxCapacity)
            .InclusiveBetween(1, 100)
            .WithMessage("MaxCapacity must be between 1 and 100.");

        RuleFor(x => x.DaysOfWeek)
            .Must(days => days is null || days.Count > 0)
            .WithMessage("DaysOfWeek cannot be empty when supplied.");
    }
}
