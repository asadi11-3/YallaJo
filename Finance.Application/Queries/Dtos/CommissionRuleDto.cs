namespace Finance.Application.Queries.Dtos;

public sealed record CommissionRuleDto(
    Guid Id,
    string Tier,
    decimal MinMonthlyRevenue,
    decimal? MaxMonthlyRevenue,
    string Currency,
    decimal Percentage,
    bool IsActive,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
