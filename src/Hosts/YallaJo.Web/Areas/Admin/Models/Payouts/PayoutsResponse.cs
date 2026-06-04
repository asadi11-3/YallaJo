namespace YallaJo.Web.Areas.Admin.Models.Payouts;

public sealed class PayoutPageResponse
{
    public IReadOnlyList<PayoutItemResponse> Items { get; set; } = [];
    public Guid? NextCursor { get; set; }
}

public sealed class PayoutItemResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string Currency { get; set; } = "JOD";
    public DateOnly BatchPeriodStart { get; set; }
    public DateOnly BatchPeriodEnd { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = "";
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? FailureReason { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
