using FluentValidation;

namespace ContentPlaces.Application.Commands.Business.RequestMoreDocs;

public sealed class RequestMoreDocsCommandValidator : AbstractValidator<RequestMoreDocsCommand>
{
    public RequestMoreDocsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason for requesting additional documents is required.")
            .MaximumLength(1000);

        RuleFor(x => x.ReviewedByUserId).NotEmpty();
    }
}
