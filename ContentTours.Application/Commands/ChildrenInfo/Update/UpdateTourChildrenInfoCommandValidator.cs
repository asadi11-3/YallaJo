using FluentValidation;

namespace ContentTours.Application.Commands.ChildrenInfo.Update;

public sealed class UpdateTourChildrenInfoCommandValidator : AbstractValidator<UpdateTourChildrenInfoCommand>
{
    public UpdateTourChildrenInfoCommandValidator()
    {
        RuleFor(x => x.TourId)
            .NotEmpty();

        RuleFor(x => x.MinChildAge)
            .InclusiveBetween(0, 18)
            .When(x => x.MinChildAge.HasValue);

        RuleFor(x => x.MaxChildAge)
            .InclusiveBetween(0, 18)
            .GreaterThan(x => x.MinChildAge)
            .When(x => x.MaxChildAge.HasValue && x.MinChildAge.HasValue)
            .WithMessage("MaxChildAge must be greater than MinChildAge.");

        RuleFor(x => x.ChildFacilities)
            .MaximumLength(500)
            .When(x => x.ChildFacilities != null);
    }
}
