using FluentValidation;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Update;

public sealed class UpdateServiceItemCommandValidator : AbstractValidator<UpdateServiceItemCommand>
{
    public UpdateServiceItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Service Item ID is required.");

        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("Business ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("Max capacity must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be exactly 3 characters (e.g., JOD).");

        // RuleFor(x => x.Category).IsInEnum().WithMessage("Invalid category.");
    }
}
