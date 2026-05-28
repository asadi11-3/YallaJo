using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.DeactivateSeasonalityRule;

public sealed class DeactivateSeasonalityRuleCommandHandler(
    ISeasonalityRuleRepository ruleRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeactivateSeasonalityRuleCommandHandler> logger) : ICommandHandler<DeactivateSeasonalityRuleCommand>
{
    public async Task<Result> Handle(DeactivateSeasonalityRuleCommand request, CancellationToken ct)
    {
        var rule = await ruleRepository.GetByIdAsync(request.RuleId, ct).ConfigureAwait(false);
        if (rule is null)
            return Result.Failure(new Error("SeasonalityRule.NotFound", "Seasonality rule was not found."), Outcome.NotFound);

        rule.Deactivate();
        ruleRepository.Update(rule);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync("analytics:seasonality", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"analytics:seasonality:{rule.PlaceId:N}", ct).ConfigureAwait(false);
        logger.LogInformation("Deactivated analytics seasonality rule {RuleId}", request.RuleId);
        return Result.Success();
    }
}
