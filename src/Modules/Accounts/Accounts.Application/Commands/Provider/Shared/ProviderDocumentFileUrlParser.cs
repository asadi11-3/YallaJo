namespace Accounts.Application.Commands.Provider.Shared;

/// <summary>
/// Outcome of <see cref="ProviderDocumentFileUrlParser.Parse"/>. On success the
/// <see cref="StorageKey"/>, <see cref="Extension"/>, and <see cref="ContentType"/>
/// are populated. On failure <see cref="SkipReason"/> identifies why a backfill
/// caller should skip the row. SkipReason values are part of the public
/// BackfillReport contract — do NOT rename without updating the tests.
/// </summary>
internal sealed record ProviderDocumentParseResult(
    bool IsSuccess,
    string? StorageKey,
    string? Extension,
    string? ContentType,
    string? SkipReason);

/// <summary>
/// Parses the relative <c>/uploads/provider-application-documents/{guidv7}{ext}</c>
/// URL stored on legacy <see cref="Accounts.Domain.Entities.ProviderDocument.FileUrl"/>
/// rows into its physical <c>StorageKey</c> + content-type for the Patch 2B
/// backfill. Pure function — no I/O, no DbContext, no logging.
/// <para>
/// Allow-list (matches Patch 1C upload validator): PDF, JPEG, PNG only.
/// </para>
/// </summary>
internal static class ProviderDocumentFileUrlParser
{
    private const string DocumentsFolder = "provider-application-documents";

    public static ProviderDocumentParseResult Parse(string? fileUrl, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return Fail("NoFileUrl");

        // baseUrl convention: cfg["FileStorage:BaseUrl"]?.TrimEnd('/') ?? "/uploads"
        var normalizedBase = (baseUrl ?? "/uploads").TrimEnd('/');
        var expectedPrefix = $"{normalizedBase}/{DocumentsFolder}/";

        if (!fileUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return Fail("InvalidPrefix");

        // Strip prefix; tail must be a single segment {guidv7}{ext}.
        var tail = fileUrl[expectedPrefix.Length..].TrimStart('/');

        if (string.IsNullOrWhiteSpace(tail))
            return Fail("InvalidPath");

        // Reject traversal / multi-segment paths / backslashes / null bytes.
        if (tail.Contains("..", StringComparison.Ordinal)
            || tail.Contains('\\')
            || tail.Contains('/')
            || tail.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return Fail("InvalidPath");
        }

        var extension = Path.GetExtension(tail).ToLowerInvariant();
        var contentType = ResolveContentType(extension);
        if (contentType is null)
            return Fail("InvalidExtension");

        var storageKey = $"{DocumentsFolder}/{tail}";
        return new ProviderDocumentParseResult(
            IsSuccess: true,
            StorageKey: storageKey,
            Extension: extension,
            ContentType: contentType,
            SkipReason: null);
    }

    /// <summary>Allow-list of legacy provider document content types (Patch 1C parity).</summary>
    private static string? ResolveContentType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => null,
    };

    private static ProviderDocumentParseResult Fail(string reason)
        => new(IsSuccess: false, StorageKey: null, Extension: null, ContentType: null, SkipReason: reason);
}
