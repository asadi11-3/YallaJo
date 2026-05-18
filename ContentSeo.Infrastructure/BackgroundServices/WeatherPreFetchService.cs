// <copyright file="WeatherPreFetchService.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.BackgroundServices;

using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Weather;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class WeatherPreFetchService(
    IServiceScopeFactory scopeFactory,
    IOptions<WeatherOptions> options,
    ILogger<WeatherPreFetchService> logger) : BackgroundService
{
    private readonly WeatherOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduledHour = Math.Clamp(this.options.PreFetchHourUtc, 0, 23);
        var dailyBudget = Math.Max(0, this.options.DailyBudget);

        logger.LogInformation(
            "WeatherPreFetchService started; runs daily at {Hour:00}:00 UTC, daily budget={Budget}",
            scheduledHour,
            dailyBudget);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayToNextScheduledHour(DateTime.UtcNow, scheduledHour);
            logger.LogInformation("WeatherPreFetchService sleeping for {Delay} until next {Hour:00}:00 UTC", delay, scheduledHour);

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var provider = scope.ServiceProvider.GetRequiredService<IWeatherProvider>();
                var budgetGate = scope.ServiceProvider.GetRequiredService<IWeatherBudgetGate>();
                if (!provider.IsAvailable)
                {
                    logger.LogInformation("WeatherPreFetchService: provider not available, skipping daily pre-fetch.");
                    continue;
                }

                // Real implementation would resolve IPlaceQueryService.GetTop50ByPopularityAsync(ct)
                // and iterate within the daily budget. For Wave-4, this is a no-op placeholder
                // honoring the configured budget value; the scoped gate is resolved here and
                // would be checked before each upstream provider call.
                logger.LogInformation(
                    "WeatherPreFetchService: daily run executed (budget={Budget}); budget gate {GateType} ready for provider calls.",
                    dailyBudget,
                    budgetGate.GetType().Name);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "WeatherPreFetchService iteration failed; will retry tomorrow.");
            }
        }

        logger.LogInformation("WeatherPreFetchService stopped.");
    }

    private static TimeSpan ComputeDelayToNextScheduledHour(DateTime nowUtc, int hourUtc)
    {
        var todayAtHour = nowUtc.Date.AddHours(hourUtc);
        if (nowUtc >= todayAtHour)
        {
            todayAtHour = todayAtHour.AddDays(1);
        }

        return todayAtHour - nowUtc;
    }
}
