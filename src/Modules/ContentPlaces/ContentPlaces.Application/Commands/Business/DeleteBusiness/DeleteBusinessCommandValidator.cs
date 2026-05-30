using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.DeleteBusiness;

public sealed class DeleteBusinessCommandValidator : AbstractValidator<DeleteBusinessCommand>
{
    public DeleteBusinessCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
