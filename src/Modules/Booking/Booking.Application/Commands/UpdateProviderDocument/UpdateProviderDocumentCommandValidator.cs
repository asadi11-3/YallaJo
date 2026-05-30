using Booking.Domain.Extensions;
using FluentValidation;

namespace Booking.Application.Commands.UpdateProviderDocument;

public sealed class UpdateProviderDocumentCommandValidator : AbstractValidator<UpdateProviderDocumentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
    ];

    public UpdateProviderDocumentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x)
            .Must(cmd => cmd.FileStream is not null || cmd.ExpiresAt.HasValue)
            .WithMessage("At least one of File or ExpiresAt must be provided.");

        When(x => x.FileStream is not null, () =>
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("File name is required when a file is uploaded.")
                .MaximumLength(500);

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("Content type is required when a file is uploaded.")
                .Must(ct => ct is not null && AllowedContentTypes.Contains(NormalizeContentType(ct)))
                .WithMessage("Only PDF, JPEG, and PNG uploads are accepted.");

            RuleFor(x => x.FileSize)
                .NotNull().WithMessage("File size is required when a file is uploaded.")
                .Must(s => s is > 0).WithMessage("File must not be empty.")
                .Must(s => s is <= DocumentTypeExtensions.MaxUploadBytes)
                .WithMessage($"File exceeds the {DocumentTypeExtensions.MaxUploadBytes / (1024 * 1024)} MB limit.");
        });

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
