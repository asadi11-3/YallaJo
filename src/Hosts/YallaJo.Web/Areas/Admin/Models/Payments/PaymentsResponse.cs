namespace YallaJo.Web.Areas.Admin.Models.Payments;

// Mirrors Finance.Application.Earnings.AdminFinanceDashboardDto
// returned by GET /api/v1/finance/admin/dashboard.
public sealed class AdminFinanceDashboardResponse
{
    public decimal GrossRevenue { get; set; }
    public decimal PlatformCommissionEstimate { get; set; }
    public decimal ProviderNetEstimate { get; set; }
    public int CompletedPaymentCount { get; set; }
    public int OpenDisputeCount { get; set; }
    public string Currency { get; set; } = "JOD";
}

// Mirrors Finance.Application.Queries.Dtos.PaymentPageDto
// returned by GET /api/v1/payments/admin/all.
public sealed class PaymentPageResponse
{
    public IReadOnlyList<PaymentResponse> Items { get; set; } = [];
    public Guid? NextCursor { get; set; }
    public int? TotalCount { get; set; }
}

// Mirrors Finance.Application.Queries.Dtos.PaymentDto.
public sealed class PaymentResponse
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
