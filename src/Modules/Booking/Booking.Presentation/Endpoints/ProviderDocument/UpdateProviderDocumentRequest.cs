using Booking.Application.Commands.UpdateProviderDocument;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Presentation.Endpoints.ProviderDocument;

public sealed class UpdateProviderDocumentRequest
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }

    [FromForm(Name = "expiresAt")]
    public DateOnly? ExpiresAt { get; init; }
    [FromForm(Name = "rowVersion")]
    public string? RowVersion { get; init; }

    public UpdateProviderDocumentCommand BuildCommand(Guid id, out Stream? leasedStream)
    {
        leasedStream = null;
        Stream? fileStream = null;
        string? fileName = null;
        string? contentType = null;
        long? fileSize = null;

        if (File is not null)
        {
            fileStream = File.OpenReadStream();
            leasedStream = fileStream;
            fileName = File.FileName;
            contentType = File.ContentType;
            fileSize = File.Length;
        }

        byte[] decodedRowVersion;
        try
        {
            decodedRowVersion = string.IsNullOrWhiteSpace(RowVersion)
                ? []
                : Convert.FromBase64String(RowVersion);
        }
        catch (FormatException)
        {
            decodedRowVersion = [];
        }

        return new UpdateProviderDocumentCommand(
            Id: id,
            FileStream: fileStream,
            FileName: fileName,
            ContentType: contentType,
            FileSize: fileSize,
            ExpiresAt: ExpiresAt,
            RowVersion: decodedRowVersion);
    }
}
