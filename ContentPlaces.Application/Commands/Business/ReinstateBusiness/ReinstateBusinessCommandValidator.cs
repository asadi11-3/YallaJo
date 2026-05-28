using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.ReinstateBusiness;

public sealed class ReinstateBusinessCommandValidator : AbstractValidator<ReinstateBusinessCommand>
{
    public ReinstateBusinessCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReinstatedByUserId).NotEmpty();
    }
}
