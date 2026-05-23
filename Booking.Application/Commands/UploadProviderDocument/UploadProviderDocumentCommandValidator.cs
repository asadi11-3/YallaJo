using Booking.Domain.Extensions;
using FluentValidation;

namespace Booking.Application.Commands.UploadProviderDocument;

public sealed class UploadProviderDocumentCommandValidator : AbstractValidator<UploadProviderDocumentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
    ];

    public UploadProviderDocumentCommandValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type is not a recognised DocumentType.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File payload is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(500);

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(ct => AllowedContentTypes.Contains(NormalizeContentType(ct)))
            .WithMessage("Only PDF, JPEG, and PNG uploads are accepted.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(DocumentTypeExtensions.MaxUploadBytes)
            .WithMessage($"File exceeds the {DocumentTypeExtensions.MaxUploadBytes / (1024 * 1024)} MB limit.");

        RuleFor(x => x.ExpiresAt)
            .Must((cmd, expiresAt) => !cmd.Type.RequiresExpiry() || expiresAt.HasValue)
            .WithMessage(cmd => $"DocumentType {cmd.Type} requires an ExpiresAt value.");

        RuleFor(x => x.ExpiresAt!.Value)
            .Must(d => d > DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("ExpiresAt must be in the future.");
    }

    private static string NormalizeContentType(string contentType)
    {
        var semicolon = contentType.IndexOf(';');
        var mime = semicolon >= 0 ? contentType[..semicolon] : contentType;
        return mime.Trim().ToLowerInvariant();
    }
}
