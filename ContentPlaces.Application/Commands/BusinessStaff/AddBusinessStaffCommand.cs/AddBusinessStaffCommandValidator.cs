using FluentValidation;

namespace ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;

public sealed class AddBusinessStaffCommandValidator
    : AbstractValidator<AddBusinessStaffCommand>
{
    public AddBusinessStaffCommandValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role)
    .IsInEnum()
    .WithMessage("Invalid role value");
    }
}
