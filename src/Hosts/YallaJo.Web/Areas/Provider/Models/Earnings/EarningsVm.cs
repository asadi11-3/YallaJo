namespace YallaJo.Web.Areas.Provider.Models.Earnings;

public sealed class EarningsVm
{
    public decimal GrossTotal { get; init; }
    public decimal NetEstimateTotal { get; init; }
    public int PaymentCount { get; init; }
    public string Currency { get; init; } = string.Empty;

    public IReadOnlyList<EarningRowVm> Earnings { get; init; } = [];
    public IReadOnlyList<PayoutRowVm> Payouts { get; init; } = [];
    public IReadOnlyList<DisputeRowVm> Disputes { get; init; } = [];

    public bool HasEarnings => Earnings.Count > 0;
    public bool HasPayouts => Payouts.Count > 0;
    public bool HasDisputes => Disputes.Count > 0;
}

public sealed class EarningRowVm
{
    public Guid BookingId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal NetEstimate { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
}

public sealed class PayoutRowVm
{
    public Guid Id { get; init; }
    public string? Status { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateOnly BatchPeriodStart { get; init; }
    public DateOnly BatchPeriodEnd { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal NetAmount { get; init; }
    public int ItemCount { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class DisputeRowVm
{
    public string Reason { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Status { get; init; }
    public string? Resolution { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
