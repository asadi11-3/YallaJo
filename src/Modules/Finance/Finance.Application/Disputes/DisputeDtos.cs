using Finance.Domain.Enums;

namespace Finance.Application.Disputes;

/// <summary>
/// Per-status dispute counts for the admin queue counted tabs.
/// Covers every <see cref="DisputeStatus"/> value.
/// </summary>
public sealed record DisputeStatusCountsDto(int Open, int UnderReview, int Resolved, int Escalated, int Closed);

public sealed record DisputeDto(
    Guid Id,
    Guid PaymentId,
    Guid UserId,
    string Reason,
    string Description,
    DisputeStatus Status,
    DisputeResolution? Resolution,
    DateTime? ResolvedAt,
    Guid? ResolvedByUserId,
    string? ResolutionNotes,
    DateTime CreatedAt);
