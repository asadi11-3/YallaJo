using Finance.Application.Queries.Dtos;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetCommissionRules;

public sealed class GetCommissionRulesQueryHandler(ICommissionRuleRepository commissionRuleRepository)
    : IRequestHandler<GetCommissionRulesQuery, Result<IReadOnlyList<CommissionRuleDto>>>
{
    public async Task<Result<IReadOnlyList<CommissionRuleDto>>> Handle(GetCommissionRulesQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // GetActiveAsync ignores soft-deletes via query filter; if IncludeInactive we
        // need to expand to all rules. For now we use active only — IncludeInactive is reserved.
        var rules = await commissionRuleRepository.GetActiveAsync(ct);

        var filtered = rules.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Tier))
        {
            var t = request.Tier.Trim();
            filtered = filtered.Where(r => string.Equals(r.Tier, t, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            var c = request.Currency.ToUpperInvariant();
            filtered = filtered.Where(r => r.Currency == c);
        }

        var dto = filtered.Select(MapToDto).ToList();
        return Result.Success<IReadOnlyList<CommissionRuleDto>>(dto);
    }

    internal static CommissionRuleDto MapToDto(CommissionRule r) => new(
        r.Id,
        r.Tier,
        r.MinMonthlyRevenue,
        r.MaxMonthlyRevenue,
        r.Currency,
        r.Percentage,
        r.IsActive,
        r.Notes,
        r.CreatedAt,
        r.UpdatedAt);
}
