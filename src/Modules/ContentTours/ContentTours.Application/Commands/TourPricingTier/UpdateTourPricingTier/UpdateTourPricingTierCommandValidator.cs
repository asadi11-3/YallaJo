using ContentTours.Domain.Enums;
using FluentValidation;

namespace ContentTours.Application.Commands.TourPricingTier.UpdateTourPricingTier;

public sealed class UpdateTourPricingTierCommandValidator : AbstractValidator<UpdateTourPricingTierCommand>
{
    public UpdateTourPricingTierCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
        RuleFor(x => x.TierId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ParticipantType)
            .IsInEnum()
            .WithMessage($"ParticipantType must be one of: {string.Join(", ", Enum.GetNames<ParticipantType>())}.");
        RuleFor(x => x.MinParticipants).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MaxParticipants)
            .GreaterThan(x => x.MinParticipants)
            .When(x => x.MaxParticipants.HasValue);
    }
}
