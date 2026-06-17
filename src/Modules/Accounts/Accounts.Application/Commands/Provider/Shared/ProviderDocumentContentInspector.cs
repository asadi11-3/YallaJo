using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.Shared;

/// <summary>
/// Patch 1C — defence-in-depth content validation for sensitive provider
/// application documents (national IDs, business licenses — PII).
///
/// The upload pipeline already validates extension, declared ContentType, and
/// size (see <c>UploadProviderDocumentCommandValidator</c>). Those are all
/// CLIENT-CONTROLLED and trivially spoofable. This inspector adds the missing
/// layer: it reads the file's real magic bytes and requires the detected format
/// to agree with BOTH the declared ContentType AND the file extension.
///
/// Only the three formats the validator allow-lists are accepted: PDF, JPEG, PNG.
///
/// All failures return a safe, generic validation error. No physical path,
/// storage key, server path, or internal exception detail is ever surfaced.
/// </summary>
internal static class ProviderDocumentContentInspector
{
    /// <summary>Number of leading bytes inspected for a signature match.</summary>
    private const int SignatureProbeLength = 16;

    private static readonly Error MismatchError = Error.Validation(
        "file",
        "The uploaded file content does not match its type. Allowed document types are PDF, JPEG, and PNG.");

    private static readonly Error UnreadableError = Error.Validation(
        "file",
        "The uploaded file could not be read for validation.");

    internal enum DetectedDocumentType
    {
        Unknown = 0,
        Pdf,
        Jpeg,
        Png,
    }

    /// <summary>
    /// Validates that <paramref name="content"/>'s real signature is an allowed
    /// document format and that it is consistent with the declared content type
    /// and file extension. The stream position is restored before returning, so
    /// the caller can hand the same stream to storage afterwards.
    /// </summary>
    /// <remarks>The stream MUST be seekable (callers buffer non-seekable uploads first).</remarks>
    public static async Task<Result> ValidateAsync(
        Stream content,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        if (content is null || !content.CanRead || !content.CanSeek)
            return Result.Failure(UnreadableError, Outcome.Invalid);

        DetectedDocumentType detected;
        var originalPosition = content.Position;
        var buffer = new byte[SignatureProbeLength];

        try
        {
            content.Seek(0, SeekOrigin.Begin);
            var bytesRead = await ReadAtLeastAsync(content, buffer, ct);
            detected = Detect(buffer, bytesRead);
        }
        catch (IOException)
        {
            return Result.Failure(UnreadableError, Outcome.Invalid);
        }
        finally
        {
            // Always rewind so the storage provider reads the file from the start.
            if (content.CanSeek)
                content.Seek(originalPosition, SeekOrigin.Begin);
        }

        if (detected == DetectedDocumentType.Unknown)
            return Result.Failure(MismatchError, Outcome.Invalid);

        // Triple cross-check: the real signature must agree with the declared
        // ContentType AND the file extension. Any disagreement is a rejection.
        if (!IsContentTypeCompatible(detected, contentType) ||
            !IsExtensionCompatible(detected, fileName))
        {
            return Result.Failure(MismatchError, Outcome.Invalid);
        }

        return Result.Success();
    }

    private static async Task<int> ReadAtLeastAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct);
            if (read == 0)
                break;
            total += read;
        }

        return total;
    }

    private static DetectedDocumentType Detect(byte[] buffer, int bytesRead)
    {
        // PDF: "%PDF" (25 50 44 46)
        if (HasPrefix(buffer, bytesRead, 0x25, 0x50, 0x44, 0x46))
            return DetectedDocumentType.Pdf;

        // JPEG: FF D8 FF
        if (HasPrefix(buffer, bytesRead, 0xFF, 0xD8, 0xFF))
            return DetectedDocumentType.Jpeg;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (HasPrefix(buffer, bytesRead, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            return DetectedDocumentType.Png;

        return DetectedDocumentType.Unknown;
    }

    private static bool IsContentTypeCompatible(DetectedDocumentType detected, string contentType)
    {
        var mime = NormalizeContentType(contentType);
        return detected switch
        {
            DetectedDocumentType.Pdf => mime == "application/pdf",
            DetectedDocumentType.Jpeg => mime == "image/jpeg",
            DetectedDocumentType.Png => mime == "image/png",
            _ => false,
        };
    }

    private static bool IsExtensionCompatible(DetectedDocumentType detected, string fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return detected switch
        {
            DetectedDocumentType.Pdf => extension == ".pdf",
            DetectedDocumentType.Jpeg => extension is ".jpg" or ".jpeg",
            DetectedDocumentType.Png => extension == ".png",
            _ => false,
        };
    }

    private static string NormalizeContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return string.Empty;

        var separatorIndex = contentType.IndexOf(';');
        var mime = separatorIndex >= 0 ? contentType[..separatorIndex] : contentType;
        return mime.Trim().ToLowerInvariant();
    }

    private static bool HasPrefix(byte[] buffer, int bytesRead, params byte[] signature)
    {
        if (bytesRead < signature.Length)
            return false;

        for (var i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
                return false;
        }

        return true;
    }
}
