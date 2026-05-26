using FluentValidation;

namespace ContentTours.Application.Commands.TourProposal.Create;

public sealed class CreateTourProposalCommandValidator : AbstractValidator<CreateTourProposalCommand>
{
    public CreateTourProposalCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.ShortDescription).MaximumLength(500);
        RuleFor(x => x.PlaceId).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 2880);
        RuleFor(x => x.MaxGroupSize).InclusiveBetween(1, 100);
        RuleFor(x => x.BasePrice).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
    }
}
