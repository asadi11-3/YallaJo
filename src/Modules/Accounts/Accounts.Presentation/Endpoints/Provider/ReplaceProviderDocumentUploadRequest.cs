using Accounts.Application.Commands.Provider.ReplaceDocumentUpload;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.Presentation.Endpoints.Provider;

/// <summary>
/// Multipart payload for POST /api/v1/provider/documents/{id}/replace-upload.
/// Additive alternative to the URL-based PUT /documents/{id} replace route —
/// lets the provider upload an actual file instead of typing metadata by hand.
/// </summary>
public sealed class ReplaceProviderDocumentUploadRequest
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }

    [FromForm(Name = "expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    public ReplaceProviderDocumentUploadCommand? TryBuildCommand(Guid documentId, out Stream? leasedStream)
    {
        leasedStream = null;
        if (File is null)
            return null;

        var stream = File.OpenReadStream();
        leasedStream = stream;
        return new ReplaceProviderDocumentUploadCommand(
            DocumentId: documentId,
            FileStream: stream,
            FileName: File.FileName,
            ContentType: File.ContentType,
            FileSizeBytes: File.Length,
            ExpiresAt: ExpiresAt);
    }
}
