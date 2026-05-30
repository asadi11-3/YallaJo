using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CreateSeasonalityRule;

public sealed class CreateSeasonalityRuleCommandHandler(
    ISeasonalityRuleRepository ruleRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateSeasonalityRuleCommandHandler> logger) : ICommandHandler<CreateSeasonalityRuleCommand, CreateSeasonalityRuleResult>
{
    public async Task<Result<CreateSeasonalityRuleResult>> Handle(CreateSeasonalityRuleCommand request, CancellationToken ct)
    {
        var rule = SeasonalityRule.Create(request.PlaceId, request.MonthStart, request.MonthEnd, request.Multiplier, request.Description);
        ruleRepository.Add(rule);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync("analytics:seasonality", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"analytics:seasonality:{request.PlaceId:N}", ct).ConfigureAwait(false);
        logger.LogInformation("Created analytics seasonality rule {RuleId} for place {PlaceId}", rule.Id, request.PlaceId);
        return Result<CreateSeasonalityRuleResult>.Success(new CreateSeasonalityRuleResult(rule.Id));
    }
}
