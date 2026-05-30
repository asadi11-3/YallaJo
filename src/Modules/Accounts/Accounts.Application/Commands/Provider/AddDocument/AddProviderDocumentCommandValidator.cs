using FluentValidation;

namespace Accounts.Application.Commands.Provider.AddDocument;

public sealed class AddProviderDocumentCommandValidator : AbstractValidator<AddProviderDocumentCommand>
{
    private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public AddProviderDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Invalid document type.");

        RuleFor(x => x.FileUrl)
            .NotEmpty().WithMessage("File URL is required.")
            .MaximumLength(2048).WithMessage("File URL must not exceed 2048 characters.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(500).WithMessage("File name must not exceed 500 characters.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than 0.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("File size must not exceed 10 MB.");
    }
}
