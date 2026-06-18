namespace Booking.Application.Queries.DownloadProviderDocument;

/// <summary>
/// Carries an authorized, open file stream for a Booking provider document.
/// <para>
/// The <see cref="Content"/> stream is OWNED by the caller (the endpoint) and MUST be
/// disposed after the response is written. No physical path or storage key is exposed —
/// only a sanitized <see cref="FileName"/> safe to use as a download name.
/// </para>
/// </summary>
public sealed record DownloadProviderDocumentResult(
    Stream Content,
    string ContentType,
    string FileName,
    long? FileSize);
