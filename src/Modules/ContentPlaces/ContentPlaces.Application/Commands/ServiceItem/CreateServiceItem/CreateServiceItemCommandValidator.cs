using FluentValidation;

namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;

public sealed class CreateServiceItemCommandValidator : AbstractValidator<CreateServiceItemCommand>
{
    public CreateServiceItemCommandValidator()
    {
        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.MaxCapacity).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
    }
}
