using ContentCore.Application.Limits;
using ContentCore.Domain.Enums;
using FluentValidation;

namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    // SVG is intentionally excluded — it can embed executable JavaScript (XSS vector)
    // and ContentCore has no sanitization pipeline.
    public static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images (no SVG)
        "image/jpeg", "image/png", "image/gif", "image/webp",
        // Videos
        "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo",
        // Audio
        "audio/mpeg", "audio/ogg", "audio/wav", "audio/webm",
        // Documents
        "application/pdf",
    };

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images (no .svg)
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        // Videos
        ".mp4", ".webm", ".mov", ".avi",
        // Audio
        ".mp3", ".ogg", ".wav",
        // Documents
        ".pdf",
    };

    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name)))
            .WithMessage(
                "File extension is not allowed or unsafe (SVG is blocked). " +
                $"Allowed: {string.Join(", ", AllowedExtensions)}.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(100)
            .Must(ct => AllowedMimeTypes.Contains(ct))
            .WithMessage(
                "Content type is not allowed or unsafe (image/svg+xml is blocked). " +
                $"Allowed: {string.Join(", ", AllowedMimeTypes)}.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("File must not be empty.");

        // Per-type size validation — single source of truth from AttachmentLimits
        RuleFor(x => x)
            .Must(cmd => cmd.FileSize <= AttachmentLimits.GetMaxFileSize(cmd.Type))
            .WithMessage(cmd =>
            {
                var maxBytes = AttachmentLimits.GetMaxFileSize(cmd.Type);
                var mb = maxBytes / (1024 * 1024);
                return $"File size exceeds the {cmd.Type} limit of {mb} MB.";
            });

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
