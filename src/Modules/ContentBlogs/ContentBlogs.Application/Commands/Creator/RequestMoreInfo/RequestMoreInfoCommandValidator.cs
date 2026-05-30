using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.RequestMoreInfo;

public sealed class RequestMoreInfoCommandValidator
    : AbstractValidator<RequestMoreInfoCommand>
{
    public RequestMoreInfoCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.AdminNote)
            .NotEmpty().WithMessage("Admin note is required.")
            .MaximumLength(2000);
    }
}
