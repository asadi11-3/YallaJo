namespace YallaJo.Web.Areas.Admin.Models.Commissions;

public static class CommissionsMapper
{
    public static CommissionsVm ToVm(
        IReadOnlyList<CommissionRuleResponse> rules,
        string? tier,
        string? currency,
        bool includeInactive)
    {
        return new CommissionsVm
        {
            TierFilter = tier,
            CurrencyFilter = currency,
            IncludeInactive = includeInactive,
            Rules = rules.Select(r => new CommissionRuleRowVm
            {
                Id = r.Id,
                Tier = r.Tier,
                MinMonthlyRevenue = r.MinMonthlyRevenue,
                MaxMonthlyRevenue = r.MaxMonthlyRevenue,
                Currency = r.Currency,
                Percentage = r.Percentage,
                IsActive = r.IsActive,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            }).ToList(),
        };
    }
}
