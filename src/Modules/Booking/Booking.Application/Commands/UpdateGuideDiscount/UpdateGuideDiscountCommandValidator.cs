using FluentValidation;

namespace Booking.Application.Commands.UpdateGuideDiscount;

// UpdateGuideDiscountCommand had no validator, so a PUT /api/v1/booking/guide-discounts/{id}
// body with a null Name reached the handler and threw an unguarded NullReferenceException
// (GuideDiscount.Update calls name.Trim() on null) -> 500. This validator returns a clean 400
// and enforces the required Name at the API contract layer (mirrors the CreateGuideDiscount fix).
public sealed class UpdateGuideDiscountCommandValidator : AbstractValidator<UpdateGuideDiscountCommand>
{
    public UpdateGuideDiscountCommandValidator()
    {
        RuleFor(x => x.DiscountId)
            .NotEmpty().WithMessage("Discount ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A discount name is required.")
            .MaximumLength(200).WithMessage("Discount name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);
    }
}
