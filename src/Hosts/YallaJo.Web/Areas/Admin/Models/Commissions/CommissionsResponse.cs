namespace YallaJo.Web.Areas.Admin.Models.Commissions;

public sealed class CommissionRuleResponse
{
    public Guid Id { get; set; }
    public string Tier { get; set; } = string.Empty;
    public decimal MinMonthlyRevenue { get; set; }
    public decimal? MaxMonthlyRevenue { get; set; }
    public string Currency { get; set; } = "JOD";
    public decimal Percentage { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
