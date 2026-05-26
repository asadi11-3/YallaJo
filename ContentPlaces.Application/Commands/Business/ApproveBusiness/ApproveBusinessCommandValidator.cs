using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.ApproveBusiness;

public sealed class ApproveBusinessCommandValidator : AbstractValidator<ApproveBusinessCommand>
{
    public ApproveBusinessCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApprovedByUserId).NotEmpty();
    }
}
