using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.ResubmitBusiness;

public sealed class ResubmitBusinessCommandValidator : AbstractValidator<ResubmitBusinessCommand>
{
    public ResubmitBusinessCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
