using FluentValidation;
using System;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Create;

// بنورث من AbstractValidator وبنعطيه اسم الـ Command تبعنا
public sealed class CreateServiceItemCommandValidator : AbstractValidator<CreateServiceItemCommand>
{
    public CreateServiceItemCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty()
            .NotEqual(Guid.Empty).WithMessage("Business ID is required and cannot be empty.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(300).WithMessage("Service name must not exceed 300 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("Max Capacity must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3).WithMessage("Currency must be a maximum of 3 characters (e.g., JOD, USD).");

        // RuleFor(x => x.Category).IsInEnum().WithMessage("Invalid category.");
    }
}
