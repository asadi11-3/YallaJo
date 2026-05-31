using FluentValidation;

namespace Booking.Application.Commands.CreateGuideDiscount;

public sealed class CreateGuideDiscountCommandValidator : AbstractValidator<CreateGuideDiscountCommand>
{
    public CreateGuideDiscountCommandValidator()
    {
        RuleFor(x => x.DiscountType).IsInEnum();
    }
}
