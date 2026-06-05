namespace YallaJo.Web.Areas.Accounts.Models.Payments;

/// <summary>
/// Mirrors Finance PaymentDto. Status/PaymentType/PaymentMethod/GatewayProvider come back as strings.
/// </summary>
public sealed record PaymentResponse(
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

public sealed record PaymentPageResponse(
    IReadOnlyList<PaymentResponse> Items,
    Guid? NextCursor,
    int? TotalCount);
