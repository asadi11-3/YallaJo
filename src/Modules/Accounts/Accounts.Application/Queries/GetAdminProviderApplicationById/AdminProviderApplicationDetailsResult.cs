using Accounts.Domain.Enums;

namespace Accounts.Application.Queries.GetAdminProviderApplicationById;

public sealed record AdminProviderApplicationDetailsResult(
    Guid ApplicationId,
    Guid UserId,
    string ContactEmail,
    ProviderType Type,
    string BusinessName,
    string ContactPhone,
    string Address,
    string Description,
    ProviderApplicationStatus Status,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByUserId,
    string? RejectionReason,
    string? SuspensionReason,
    int ReapplicationCount,
    DateTime? CoolingPeriodEndsAt,
    IReadOnlyList<DocumentType> MissingDocumentTypes,
    IReadOnlyList<AdminProviderDocumentDto> Documents);

// Patch 2G: FileAsset V2 is now the authoritative source for document file metadata.
// The legacy on-disk FileUrl was removed from this DTO (intentional V2 breaking change).
public sealed record AdminProviderDocumentDto(
    Guid DocumentId,
    DocumentType DocumentType,
    string FileName,
    long FileSizeBytes,
    DateTime UploadedAt,
    DateTime? ExpiresAt);
