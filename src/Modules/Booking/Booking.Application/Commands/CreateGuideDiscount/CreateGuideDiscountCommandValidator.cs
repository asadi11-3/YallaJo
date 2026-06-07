using FluentValidation;

namespace Booking.Application.Commands.CreateGuideDiscount;

public sealed class CreateGuideDiscountCommandValidator : AbstractValidator<CreateGuideDiscountCommand>
{
    public CreateGuideDiscountCommandValidator()
    {
        RuleFor(x => x.DiscountType).IsInEnum();

        // Name/Currency are required strings that the domain factory trims (GuideDiscount.Create
        // does name.Trim()). Without these rules a null Name reached the handler and threw an
        // unguarded NullReferenceException -> 500. NotEmpty returns a clean 400 instead.
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A discount name is required.")
            .MaximumLength(200).WithMessage("Discount name must not exceed 200 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);
    }
}
