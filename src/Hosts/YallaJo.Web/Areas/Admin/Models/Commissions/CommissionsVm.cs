namespace YallaJo.Web.Areas.Admin.Models.Commissions;

public sealed class CommissionsVm
{
    public IReadOnlyList<CommissionRuleRowVm> Rules { get; set; } = [];
    public string? TierFilter { get; set; }
    public string? CurrencyFilter { get; set; }
    public bool IncludeInactive { get; set; }
}

public sealed class CommissionRuleRowVm
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
