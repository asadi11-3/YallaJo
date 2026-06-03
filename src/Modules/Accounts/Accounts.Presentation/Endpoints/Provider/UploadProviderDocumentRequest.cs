using Accounts.Application.Commands.Provider.UploadDocument;
using Accounts.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.Presentation.Endpoints.Provider;

public sealed class UploadProviderDocumentRequest
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }

    [FromForm(Name = "documentType")]
    public DocumentType DocumentType { get; init; }

    [FromForm(Name = "expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    public UploadProviderDocumentCommand? TryBuildCommand(out Stream? leasedStream)
    {
        leasedStream = null;
        if (File is null)
            return null;

        var stream = File.OpenReadStream();
        leasedStream = stream;
        return new UploadProviderDocumentCommand(
            DocumentType: DocumentType,
            FileStream: stream,
            FileName: File.FileName,
            ContentType: File.ContentType,
            FileSizeBytes: File.Length,
            ExpiresAt: ExpiresAt);
    }
}
