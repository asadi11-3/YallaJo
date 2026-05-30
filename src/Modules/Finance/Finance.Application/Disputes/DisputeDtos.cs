using Finance.Domain.Enums;

namespace Finance.Application.Disputes;

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
