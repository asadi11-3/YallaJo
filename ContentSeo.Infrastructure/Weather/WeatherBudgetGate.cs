using ContentSeo.Application.Interfaces;
using ContentSeo.Contracts.IntegrationEvents;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Repositories;
using ContentSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentSeo.Infrastructure.Weather;

internal sealed class WeatherBudgetGate(
    IWeatherDailyBudgetRepository budgetRepository,
    IContentSeoUnitOfWork unitOfWork,
    ContentSeoDbContext dbContext,
    IDateTimeProvider clock,
    IOptions<WeatherOptions> options,
    ILogger<WeatherBudgetGate> logger) : IWeatherBudgetGate
{
    private readonly WeatherOptions options = options.Value;

    public async Task<bool> TryConsumeAsync(CancellationToken ct = default)
    {
        try
        {
            return await TryConsumeCoreAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Weather daily budget concurrency conflict; retrying once.");
            return await TryConsumeCoreAsync(ct);
        }
    }

    private async Task<bool> TryConsumeCoreAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var dailyLimit = Math.Max(1, options.DailyBudget);

        var budget = await budgetRepository.GetByDateAsync(today, ct);
        if (budget is null)
        {
            budget = WeatherDailyBudget.Create(today, dailyLimit);
            await budgetRepository.AddAsync(budget, ct);
        }

        if (!budget.TryConsume())
        {
            if (budget.AlertSentAt is null)
            {
                budget.MarkAlertSent(now);
                dbContext.OutboxMessages.Add(OutboxMessage.Create(
                    new WeatherBudgetExhaustedIntegrationEvent(today, budget.CallsUsed, budget.DailyLimit, now)));
                await unitOfWork.SaveChangesAsync(ct);
            }

            logger.LogWarning(
                "Weather daily budget exhausted for {Date}: {CallsUsed}/{DailyLimit}",
                today,
                budget.CallsUsed,
                budget.DailyLimit);
            return false;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
