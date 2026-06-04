namespace YallaJo.Web.Areas.Admin.Models.Payments;

public sealed class PaymentsVm
{
    // KPI tiles (from the finance dashboard summary).
    public decimal GrossRevenue { get; set; }
    public decimal PlatformCommissionEstimate { get; set; }
    public decimal ProviderNetEstimate { get; set; }
    public int CompletedPaymentCount { get; set; }
    public int OpenDisputeCount { get; set; }
    public string Currency { get; set; } = "JOD";

    // Payments table.
    public IReadOnlyList<PaymentRowVm> Payments { get; set; } = [];
    public Guid? NextCursor { get; set; }
    public int? TotalCount { get; set; }

    // Echoed filters.
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
}

public sealed class PaymentRowVm
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JOD";
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string GatewayProvider { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public decimal RefundedTotal { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
