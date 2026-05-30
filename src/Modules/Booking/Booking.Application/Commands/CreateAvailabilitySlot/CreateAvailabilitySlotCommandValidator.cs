using FluentValidation;

namespace Booking.Application.Commands.CreateAvailabilitySlot;

public sealed class CreateAvailabilitySlotCommandValidator : AbstractValidator<CreateAvailabilitySlotCommand>
{
    public const int MinCapacity = 1;
    public const int MaxCapacityHardCeiling = 100;

    public CreateAvailabilitySlotCommandValidator()
    {
        RuleFor(x => x.TourId)
            .NotEmpty().WithMessage("TourId is required.");

        RuleFor(x => x.Date)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date must be today or in the future.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be strictly later than StartTime (no zero-duration slot).");

        RuleFor(x => x.MaxCapacity)
            .InclusiveBetween(MinCapacity, MaxCapacityHardCeiling)
            .WithMessage($"MaxCapacity must be between {MinCapacity} and {MaxCapacityHardCeiling}.");
    }
}
