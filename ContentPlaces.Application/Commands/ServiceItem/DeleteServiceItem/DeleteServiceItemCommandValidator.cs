using FluentValidation;

namespace ContentPlaces.Application.Commands.ServiceItem.DeleteServiceItem;

public sealed class DeleteServiceItemCommandValidator : AbstractValidator<DeleteServiceItemCommand>
{
    public DeleteServiceItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BusinessId).NotEmpty();
    }
}
