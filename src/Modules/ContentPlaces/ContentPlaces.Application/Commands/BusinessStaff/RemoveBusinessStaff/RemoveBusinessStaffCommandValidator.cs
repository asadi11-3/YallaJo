using FluentValidation;

namespace ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;

public sealed class RemoveBusinessStaffCommandValidator : AbstractValidator<RemoveBusinessStaffCommand>
{
    public RemoveBusinessStaffCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
