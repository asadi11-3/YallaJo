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

public sealed record DocumentSummary(
    Guid DocumentId,
    DocumentType DocumentType,
    string FileUrl,
    string FileName,
    DateTime? ExpiresAt);
