using FluentValidation;

namespace Accounts.Application.Commands.Admin.RequestMoreDocs;

public sealed class RequestMoreDocsCommandValidator : AbstractValidator<RequestMoreDocsCommand>
{
    public RequestMoreDocsCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");

        RuleFor(x => x.MissingDocumentTypes)
            .NotEmpty().WithMessage("At least one missing document type must be specified.");

        RuleForEach(x => x.MissingDocumentTypes)
            .IsInEnum().WithMessage("Invalid document type.");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Notes are required.")
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}
