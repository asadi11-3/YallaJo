using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetSeasonalityRules;

public sealed class GetSeasonalityRulesQueryHandler(
    ISeasonalityRuleRepository ruleRepository,
    ILogger<GetSeasonalityRulesQueryHandler> logger) : IQueryHandler<GetSeasonalityRulesQuery, GetSeasonalityRulesResult>
{
    public async Task<Result<GetSeasonalityRulesResult>> Handle(GetSeasonalityRulesQuery request, CancellationToken ct)
    {
        var rules = await ruleRepository.GetAllActiveAsync(ct).ConfigureAwait(false);
        var dtos = rules
            .Select(rule => new SeasonalityRuleDto(rule.Id, rule.PlaceId, rule.MonthStart, rule.MonthEnd, rule.Multiplier, rule.Description, rule.IsActive))
            .ToList();

        logger.LogDebug("Read {Count} active analytics seasonality rules", dtos.Count);
        return Result<GetSeasonalityRulesResult>.Success(new GetSeasonalityRulesResult(dtos));
    }
}
