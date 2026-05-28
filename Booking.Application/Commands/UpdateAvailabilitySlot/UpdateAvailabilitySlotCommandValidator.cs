using FluentValidation;

namespace Booking.Application.Commands.UpdateAvailabilitySlot;

public sealed class UpdateAvailabilitySlotCommandValidator : AbstractValidator<UpdateAvailabilitySlotCommand>
{
    public UpdateAvailabilitySlotCommandValidator()
    {
        RuleFor(x => x.SlotId)
            .NotEmpty().WithMessage("SlotId is required.");

        RuleFor(x => x.MaxCapacity)
            .InclusiveBetween(1, 100)
            .WithMessage("MaxCapacity must be between 1 and 100.");
    }
}
