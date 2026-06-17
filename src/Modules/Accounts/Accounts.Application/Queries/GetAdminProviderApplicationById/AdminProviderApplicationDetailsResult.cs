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

/// <param name="FileUrl">
/// DEPRECATED in V2: legacy on-disk URL (/uploads/provider-application-documents/...) that is
/// 404'd by the API static-file middleware (Patch 1A) and will be removed in a future patch.
/// Prefer the authorized download endpoint: GET /api/v1/provider/documents/{documentId}/download.
/// </param>
public sealed record AdminProviderDocumentDto(
    Guid DocumentId,
    DocumentType DocumentType,
    string FileUrl,
    string FileName,
    long FileSizeBytes,
    DateTime UploadedAt,
    DateTime? ExpiresAt);
