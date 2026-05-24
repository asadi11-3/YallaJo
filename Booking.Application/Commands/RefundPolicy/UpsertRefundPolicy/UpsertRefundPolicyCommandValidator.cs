using FluentValidation;

namespace Booking.Application.Commands.RefundPolicy.UpsertRefundPolicy;

public sealed class UpsertRefundPolicyCommandValidator : AbstractValidator<UpsertRefundPolicyCommand>
{
    public UpsertRefundPolicyCommandValidator()
    {
        RuleFor(x => x.TourId)
            .NotEmpty().WithMessage("TourId is required.");

        RuleFor(x => x.Tiers)
            .NotNull().WithMessage("Tiers are required.")
            .Must(t => t is not null && t.Count >= 1).WithMessage("At least one tier is required.")
            .Must(t => t is null || t.Count <= Booking.Domain.Entities.RefundPolicy.MaxTierCount)
                .WithMessage($"At most {Booking.Domain.Entities.RefundPolicy.MaxTierCount} tiers are allowed.")
            .Must(t => t is null || t.Select(x => x.HoursBeforeTour).Distinct().Count() == t.Count)
                .WithMessage("HoursBeforeTour values must be unique across tiers.");

        RuleForEach(x => x.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(t => t.HoursBeforeTour)
                .GreaterThanOrEqualTo(0).WithMessage("HoursBeforeTour must be >= 0.");
            tier.RuleFor(t => t.RefundPercent)
                .InclusiveBetween(0m, 100m).WithMessage("RefundPercent must be between 0 and 100.");
        });
    }
}
