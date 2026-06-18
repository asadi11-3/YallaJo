using System.IO;

namespace ContentBlogs.Application.Commands.Creator.UploadAvatar;

/// <summary>
/// Validates an uploaded creator-avatar image by cross-checking the declared content type,
/// the file extension, AND the actual file signature (magic bytes).
/// Only JPEG, PNG and WEBP are accepted. GIF and SVG are rejected.
/// This is intentionally ContentBlogs-local (the ContentCore attachment pipeline is a
/// separate cross-module concern and its detector is internal to that module).
/// </summary>
public static class CreatorAvatarFileValidator
{
    /// <summary>Maximum number of leading bytes inspected for signature detection.</summary>
    private const int SignatureProbeLength = 32;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
    ];

    private static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    ];

    /// <summary>
    /// Validates the supplied (seekable) stream. The stream position is reset to 0
    /// on entry; callers should reset position again before uploading.
    /// </summary>
    public static CreatorAvatarValidationResult Validate(Stream content, string? contentType, string? fileName)
    {
        var normalizedContentType = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        var extension = Path.GetExtension(fileName ?? string.Empty).Trim().ToLowerInvariant();

        // 1) Declared content type must be in the allow-list.
        if (!AllowedContentTypes.Contains(normalizedContentType))
        {
            return CreatorAvatarValidationResult.Invalid(
                "contentType",
                "Unsupported image type. Allowed types: JPEG, PNG, WEBP.");
        }

        // 2) Extension must be in the allow-list.
        if (!AllowedExtensions.Contains(extension))
        {
            return CreatorAvatarValidationResult.Invalid(
                "extension",
                "Unsupported file extension. Allowed extensions: .jpg, .jpeg, .png, .webp.");
        }

        // 3) Inspect the magic bytes.
        var signature = DetectSignature(content);
        if (signature == CreatorAvatarImageSignature.Unknown)
        {
            return CreatorAvatarValidationResult.Invalid(
                "file",
                "The file content is not a valid JPEG, PNG or WEBP image.");
        }

        // 4) Signature must match the declared content type.
        if (!SignatureMatchesContentType(signature, normalizedContentType))
        {
            return CreatorAvatarValidationResult.Invalid(
                "contentType",
                "The file content does not match the declared content type.");
        }

        // 5) Signature must match the declared extension.
        if (!SignatureMatchesExtension(signature, extension))
        {
            return CreatorAvatarValidationResult.Invalid(
                "extension",
                "The file content does not match the file extension.");
        }

        return CreatorAvatarValidationResult.Success();
    }

    private static CreatorAvatarImageSignature DetectSignature(Stream content)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var buffer = new byte[SignatureProbeLength];
        var read = 0;
        int n;
        while (read < buffer.Length &&
               (n = content.Read(buffer, read, buffer.Length - read)) > 0)
        {
            read += n;
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        // JPEG: FF D8 FF
        if (read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
        {
            return CreatorAvatarImageSignature.Jpeg;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (read >= 8 &&
            buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
            buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
        {
            return CreatorAvatarImageSignature.Png;
        }

        // WEBP: "RIFF" (bytes 0-3) .... "WEBP" (bytes 8-11)
        if (read >= 12 &&
            buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
            buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
        {
            return CreatorAvatarImageSignature.Webp;
        }

        return CreatorAvatarImageSignature.Unknown;
    }

    private static bool SignatureMatchesContentType(CreatorAvatarImageSignature signature, string contentType) =>
        signature switch
        {
            CreatorAvatarImageSignature.Jpeg => contentType == "image/jpeg",
            CreatorAvatarImageSignature.Png => contentType == "image/png",
            CreatorAvatarImageSignature.Webp => contentType == "image/webp",
            _ => false,
        };

    private static bool SignatureMatchesExtension(CreatorAvatarImageSignature signature, string extension) =>
        signature switch
        {
            CreatorAvatarImageSignature.Jpeg => extension is ".jpg" or ".jpeg",
            CreatorAvatarImageSignature.Png => extension == ".png",
            CreatorAvatarImageSignature.Webp => extension == ".webp",
            _ => false,
        };

    private enum CreatorAvatarImageSignature
    {
        Unknown = 0,
        Jpeg,
        Png,
        Webp,
    }
}

/// <summary>Outcome of <see cref="CreatorAvatarFileValidator.Validate"/>.</summary>
public sealed record CreatorAvatarValidationResult
{
    private CreatorAvatarValidationResult(bool isValid, string? field, string? message)
    {
        IsValid = isValid;
        Field = field;
        Message = message;
    }

    public bool IsValid { get; }

    /// <summary>The offending field name ("file" | "contentType" | "extension") when invalid.</summary>
    public string? Field { get; }

    /// <summary>Human-readable rejection reason when invalid.</summary>
    public string? Message { get; }

    public static CreatorAvatarValidationResult Success() => new(true, null, null);

    public static CreatorAvatarValidationResult Invalid(string field, string message) =>
        new(false, field, message);
}
