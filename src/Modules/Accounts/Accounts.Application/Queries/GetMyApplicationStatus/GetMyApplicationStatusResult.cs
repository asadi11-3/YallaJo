using Accounts.Domain.Enums;

namespace Accounts.Application.Queries.GetMyApplicationStatus;

public sealed record GetMyApplicationStatusResult(
    Guid ApplicationId,
    ProviderType Type,
    string BusinessName,
    ProviderApplicationStatus Status,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    string? RejectionReason,
    string? SuspensionReason,
    int ReapplicationCount,
    DateTime? CoolingPeriodEndsAt,
    IReadOnlyList<DocumentSummary> Documents);

/// <param name="FileUrl">
/// DEPRECATED in V2: legacy on-disk URL (/uploads/provider-application-documents/...) that is
/// 404'd by the API static-file middleware (Patch 1A) and will be removed in a future patch.
/// Prefer the authorized download endpoint: GET /api/v1/provider/documents/{documentId}/download.
/// </param>
public sealed record DocumentSummary(
    Guid DocumentId,
    DocumentType DocumentType,
    string FileUrl,
    string FileName,
    DateTime? ExpiresAt);
