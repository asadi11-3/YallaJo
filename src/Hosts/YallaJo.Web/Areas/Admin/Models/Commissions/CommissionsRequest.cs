namespace YallaJo.Web.Areas.Admin.Models.Commissions;

public sealed class CommissionsFilterRequest
{
    public string? Tier { get; set; }
    public string? Currency { get; set; }
    public bool IncludeInactive { get; set; }
}

public sealed class CreateCommissionRuleRequest
{
    public string Tier { get; set; } = string.Empty;
    public decimal MinMonthlyRevenue { get; set; }
    public decimal? MaxMonthlyRevenue { get; set; }
    public string Currency { get; set; } = "JOD";
    public decimal Percentage { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateCommissionRuleRequest
{
    public decimal MinMonthlyRevenue { get; set; }
    public decimal? MaxMonthlyRevenue { get; set; }
    public decimal Percentage { get; set; }
    public string? Notes { get; set; }
}
