namespace Finance.Application.Queries.Dtos;

/// <summary>Read projection of <see cref="Finance.Domain.Entities.Payout"/>.</summary>
public sealed record PayoutDto(
    Guid Id,
    Guid ProviderId,
    string Currency,
    DateOnly BatchPeriodStart,
    DateOnly BatchPeriodEnd,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Status,
    Guid? ApprovedByUserId,
    DateTime? ApprovedAt,
    string? GatewayPayoutId,
    DateTime? CompletedAt,
    string? FailureReason,
    Guid? BankAccountId,
    int ItemCount,
    IReadOnlyList<PayoutItemDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Read projection of one PayoutItem line.</summary>
public sealed record PayoutItemDto(
    Guid Id,
    Guid BookingId,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    Guid? CommissionRuleSnapshotId);

/// <summary>Cursor-paginated list of payouts.</summary>
public sealed record PayoutPageDto(
    IReadOnlyList<PayoutDto> Items,
    Guid? NextCursor);
