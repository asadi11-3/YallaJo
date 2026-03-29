using FluentValidation;
using System.IO;

namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    // Maximum allowed file size: 50 MB
    private const long MaxFileSizeBytes = 50L * 1024 * 1024;

    // Server-side whitelist of allowed MIME types.
    // ContentType is caller-supplied and must be validated against this list.
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        // Videos
        "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo",
        // Audio
        "audio/mpeg", "audio/ogg", "audio/wav", "audio/webm",
        // Documents
        "application/pdf",
    };

    // Server-side whitelist of allowed file extensions.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg",
        ".mp4", ".webm", ".mov", ".avi",
        ".mp3", ".ogg", ".wav",
        ".pdf",
    };

    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name)))
            .WithMessage($"File extension is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(100)
            .Must(ct => AllowedMimeTypes.Contains(ct))
            .WithMessage($"Content Type is not allowed. Allowed: {string.Join(", ", AllowedMimeTypes)}.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.EntityType)
            .IsInEnum();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
