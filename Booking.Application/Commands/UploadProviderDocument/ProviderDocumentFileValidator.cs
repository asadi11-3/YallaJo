namespace Booking.Application.Commands.UploadProviderDocument;

internal static class ProviderDocumentFileValidator
{
    public const int SignatureProbeLength = 32;

    public enum DetectedFileType
    {
        Unknown = 0,
        Pdf,
        Jpeg,
        Png,
    }

    public sealed record FileDetectionResult(
        DetectedFileType DetectedType,
        bool MimeMatches,
        bool ExtensionMatches)
    {
        public bool IsAcceptable
            => DetectedType != DetectedFileType.Unknown && MimeMatches && ExtensionMatches;
    }

    public static async Task<FileDetectionResult> DetectAsync(
        Stream stream,
        string contentType,
        string fileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(fileName);

        if (!stream.CanRead || !stream.CanSeek)
        {
            return new FileDetectionResult(DetectedFileType.Unknown, MimeMatches: false, ExtensionMatches: false);
        }

        var buffer = new byte[SignatureProbeLength];
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, SignatureProbeLength), cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead <= 0)
            {
                return new FileDetectionResult(DetectedFileType.Unknown, MimeMatches: false, ExtensionMatches: false);
            }

            var detected = DetectFromSignature(buffer, bytesRead);
            var mime = NormalizeContentType(contentType);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            return new FileDetectionResult(
                DetectedType: detected,
                MimeMatches: IsMimeCompatible(detected, mime),
                ExtensionMatches: IsExtensionCompatible(detected, ext));
        }
        finally
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
    }

    private static DetectedFileType DetectFromSignature(byte[] buffer, int bytesRead)
    {
        if (HasPrefix(buffer, bytesRead, 0x25, 0x50, 0x44, 0x46))
        {
            return DetectedFileType.Pdf; // "%PDF"
        }

        if (HasPrefix(buffer, bytesRead, 0xFF, 0xD8, 0xFF))
        {
            return DetectedFileType.Jpeg;
        }

        if (HasPrefix(buffer, bytesRead, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
        {
            return DetectedFileType.Png;
        }

        return DetectedFileType.Unknown;
    }

    private static bool IsMimeCompatible(DetectedFileType detected, string mime)
        => detected switch
        {
            DetectedFileType.Pdf => mime == "application/pdf",
            DetectedFileType.Jpeg => mime is "image/jpeg" or "image/jpg",
            DetectedFileType.Png => mime == "image/png",
            _ => false,
        };

    private static bool IsExtensionCompatible(DetectedFileType detected, string ext)
        => detected switch
        {
            DetectedFileType.Pdf => ext == ".pdf",
            DetectedFileType.Jpeg => ext is ".jpg" or ".jpeg",
            DetectedFileType.Png => ext == ".png",
            _ => false,
        };

    private static string NormalizeContentType(string contentType)
    {
        var semicolon = contentType.IndexOf(';');
        var mime = semicolon >= 0 ? contentType[..semicolon] : contentType;
        return mime.Trim().ToLowerInvariant();
    }

    private static bool HasPrefix(byte[] buffer, int bytesRead, params byte[] signature)
    {
        if (bytesRead < signature.Length)
        {
            return false;
        }

        for (var i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }
}
