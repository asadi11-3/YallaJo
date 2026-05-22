namespace Finance.Application.Queries.Dtos;

/// <summary>
/// Read-model projection of a Payment row.
/// </summary>
public sealed record PaymentDto(
    Guid Id,
    Guid UserId,
    Guid ProviderId,
    Guid? BookingId,
    Guid? OriginalPaymentId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string PaymentType,
    string Status,
    string GatewayProvider,
    string? GatewayTransactionId,
    string? RedirectUrl,
    string RecipientAccount,
    DateTime? PaidAt,
    DateTime? ExpiresAt,
    DateTime? EscrowReleaseEligibleAt,
    decimal RefundedTotal,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record PaymentPageDto(
    IReadOnlyList<PaymentDto> Items,
    Guid? NextCursor,
    int? TotalCount);
