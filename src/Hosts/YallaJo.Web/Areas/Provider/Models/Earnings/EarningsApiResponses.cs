namespace YallaJo.Web.Areas.Provider.Models.Earnings;

public sealed class GuideEarningsSummaryResponse
{
    public decimal GrossTotal { get; init; }
    public decimal NetEstimateTotal { get; init; }
    public int PaymentCount { get; init; }
    public string Currency { get; init; } = string.Empty;
}

// [Backend] B4 — mirror of Finance ProviderEarningsSummaryDto (GET /api/v1/finance/provider/summary).
public sealed class ProviderEarningsSummaryResponse
{
    public decimal GrossTotal { get; init; }
    public decimal NetEarnings { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal PendingPayout { get; init; }
    public decimal TotalCommission { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public sealed class GuideEarningResponse
{
    public Guid PaymentId { get; init; }
    public Guid BookingId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal NetEstimate { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
}

public sealed class PayoutPageResponse
{
    public IReadOnlyList<PayoutResponse> Items { get; init; } = [];
    public Guid? NextCursor { get; init; }
}

public sealed class PayoutResponse
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateOnly BatchPeriodStart { get; init; }
    public DateOnly BatchPeriodEnd { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal NetAmount { get; init; }
    public string? Status { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? GatewayPayoutId { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? FailureReason { get; init; }
    public Guid? BankAccountId { get; init; }
    public int ItemCount { get; init; }
    public IReadOnlyList<PayoutItemResponse> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class PayoutItemResponse
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal NetAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public Guid? CommissionRuleSnapshotId { get; init; }
}

public sealed class DisputeResponse
{
    public Guid Id { get; init; }
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Status { get; init; }
    public string? Resolution { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public Guid? ResolvedByUserId { get; init; }
    public string? ResolutionNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}
