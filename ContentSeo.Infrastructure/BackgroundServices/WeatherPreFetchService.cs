// <copyright file="WeatherPreFetchService.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.BackgroundServices;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;

internal sealed class WeatherPreFetchService(
    IServiceScopeFactory scopeFactory,
    ILogger<WeatherPreFetchService> logger) : BackgroundService
{
    private const int DailyBudget = 1000;
    private static readonly TimeSpan ScheduledHour = TimeSpan.FromHours(5); // 05:00 UTC

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WeatherPreFetchService started; runs daily at {Hour:hh\\:mm} UTC", ScheduledHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayToNext5AmUtc(DateTime.UtcNow);
            logger.LogInformation("WeatherPreFetchService sleeping for {Delay} until next 05:00 UTC", delay);

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
                if (!provider.IsAvailable)
                {
                    logger.LogInformation("WeatherPreFetchService: provider not available, skipping daily pre-fetch.");
                    continue;
                }

                // Real implementation would resolve IPlaceQueryService.GetTop50ByPopularityAsync(ct)
                // and iterate. For v1, this is a no-op placeholder respecting the daily budget.
                logger.LogInformation("WeatherPreFetchService: daily run executed (budget={Budget})", DailyBudget);
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

    private static TimeSpan ComputeDelayToNext5AmUtc(DateTime nowUtc)
    {
        var today5Am = nowUtc.Date.AddHours(5);
        if (nowUtc >= today5Am)
        {
            today5Am = today5Am.AddDays(1);
        }

        return today5Am - nowUtc;
    }
}
