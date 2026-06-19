using System.IO;

namespace ContentTours.Application.Commands.TourGuides.UploadAvatar;

/// <summary>
/// Validates an uploaded tour-guide avatar image by cross-checking the declared content type,
/// the file extension, AND the actual file signature (magic bytes).
/// Only JPEG, PNG and WEBP are accepted. GIF and SVG are rejected.
/// This is intentionally ContentTours-local (the ContentCore attachment pipeline is a
/// separate cross-module concern and its detector is internal to that module).
/// </summary>
public static class TourGuideAvatarFileValidator
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
    public static TourGuideAvatarValidationResult Validate(Stream content, string? contentType, string? fileName)
    {
        var normalizedContentType = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        var extension = Path.GetExtension(fileName ?? string.Empty).Trim().ToLowerInvariant();

        // 1) Declared content type must be in the allow-list.
        if (!AllowedContentTypes.Contains(normalizedContentType))
        {
            return TourGuideAvatarValidationResult.Invalid(
                "contentType",
                "Unsupported image type. Allowed types: JPEG, PNG, WEBP.");
        }

        // 2) Extension must be in the allow-list.
        if (!AllowedExtensions.Contains(extension))
        {
            return TourGuideAvatarValidationResult.Invalid(
                "extension",
                "Unsupported file extension. Allowed extensions: .jpg, .jpeg, .png, .webp.");
        }

        // 3) Inspect the magic bytes.
        var signature = DetectSignature(content);
        if (signature == TourGuideAvatarImageSignature.Unknown)
        {
            return TourGuideAvatarValidationResult.Invalid(
                "file",
                "The file content is not a valid JPEG, PNG or WEBP image.");
        }

        // 4) Signature must match the declared content type.
        if (!SignatureMatchesContentType(signature, normalizedContentType))
        {
            return TourGuideAvatarValidationResult.Invalid(
                "contentType",
                "The file content does not match the declared content type.");
        }

        // 5) Signature must match the declared extension.
        if (!SignatureMatchesExtension(signature, extension))
        {
            return TourGuideAvatarValidationResult.Invalid(
                "extension",
                "The file content does not match the file extension.");
        }

        return TourGuideAvatarValidationResult.Success();
    }

    private static TourGuideAvatarImageSignature DetectSignature(Stream content)
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
            return TourGuideAvatarImageSignature.Jpeg;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (read >= 8 &&
            buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
            buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
        {
            return TourGuideAvatarImageSignature.Png;
        }

        // WEBP: "RIFF" (bytes 0-3) .... "WEBP" (bytes 8-11)
        if (read >= 12 &&
            buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
            buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
        {
            return TourGuideAvatarImageSignature.Webp;
        }

        return TourGuideAvatarImageSignature.Unknown;
    }

    private static bool SignatureMatchesContentType(TourGuideAvatarImageSignature signature, string contentType) =>
        signature switch
        {
            TourGuideAvatarImageSignature.Jpeg => contentType == "image/jpeg",
            TourGuideAvatarImageSignature.Png => contentType == "image/png",
            TourGuideAvatarImageSignature.Webp => contentType == "image/webp",
            _ => false,
        };

    private static bool SignatureMatchesExtension(TourGuideAvatarImageSignature signature, string extension) =>
        signature switch
        {
            TourGuideAvatarImageSignature.Jpeg => extension is ".jpg" or ".jpeg",
            TourGuideAvatarImageSignature.Png => extension == ".png",
            TourGuideAvatarImageSignature.Webp => extension == ".webp",
            _ => false,
        };

    private enum TourGuideAvatarImageSignature
    {
        Unknown = 0,
        Jpeg,
        Png,
        Webp,
    }
}

/// <summary>Outcome of <see cref="TourGuideAvatarFileValidator.Validate"/>.</summary>
public sealed record TourGuideAvatarValidationResult
{
    private TourGuideAvatarValidationResult(bool isValid, string? field, string? message)
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

    public static TourGuideAvatarValidationResult Success() => new(true, null, null);

    public static TourGuideAvatarValidationResult Invalid(string field, string message) =>
        new(false, field, message);
}
