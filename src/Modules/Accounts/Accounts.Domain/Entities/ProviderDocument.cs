using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;

public sealed class ProviderDocument : BaseEntity
{
    private ProviderDocument() { } // EF Core

    public Guid ApplicationId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string FileUrl { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    // Navigation
    public ProviderApplication Application { get; private set; } = null!;

    public static ProviderDocument Create(
        Guid applicationId,
        DocumentType documentType,
        string fileUrl,
        string fileName,
        long fileSizeBytes,
        DateTime? expiresAt = null)
    {
        return new ProviderDocument
        {
            ApplicationId  = applicationId,
            DocumentType   = documentType,
            FileUrl        = fileUrl.Trim(),
            FileName       = fileName.Trim(),
            FileSizeBytes  = fileSizeBytes,
            UploadedAt     = DateTime.UtcNow,
            ExpiresAt      = expiresAt,
        };
    }

    public void Replace(string fileUrl, string fileName, long fileSizeBytes, DateTime? expiresAt = null)
    {
        FileUrl       = fileUrl.Trim();
        FileName      = fileName.Trim();
        FileSizeBytes = fileSizeBytes;
        UploadedAt    = DateTime.UtcNow;
        ExpiresAt     = expiresAt;
    }
}
