using Booking.Application.Commands.UploadProviderDocument;
using Booking.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Presentation.Endpoints.ProviderDocument;

public sealed class UploadProviderDocumentRequest
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }

    [FromForm(Name = "type")]
    public DocumentType Type { get; init; }

    [FromForm(Name = "expiresAt")]
    public DateOnly? ExpiresAt { get; init; }

    public UploadProviderDocumentCommand? TryBuildCommand(out Stream? leasedStream)
    {
        leasedStream = null;
        if (File is null)
        {
            return null;
        }

        var stream = File.OpenReadStream();
        leasedStream = stream;
        return new UploadProviderDocumentCommand(
            Type: Type,
            FileStream: stream,
            FileName: File.FileName,
            ContentType: File.ContentType,
            FileSize: File.Length,
            ExpiresAt: ExpiresAt);
    }
}
