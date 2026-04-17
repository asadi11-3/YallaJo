using FluentValidation;

namespace ContentPlaces.Application.Features.ServiceItems.Commands.Delete;

public sealed class DeleteServiceItemCommandValidator : AbstractValidator<DeleteServiceItemCommand>
{
    public DeleteServiceItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Service Item ID is required.");

        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("Business ID is required.");
    }
}
