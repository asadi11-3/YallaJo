using FluentValidation;

namespace Accounts.Application.Commands.Provider.UploadDocument;

public sealed class UploadProviderDocumentCommandValidator : AbstractValidator<UploadProviderDocumentCommand>
{
    internal static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];
    internal const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadProviderDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Invalid document type.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("A file is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(500).WithMessage("File name must not exceed 500 characters.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(ct => AllowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unsupported file type. Allowed types: PDF, JPEG, PNG.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("File size must not exceed 10 MB.");
    }
}
