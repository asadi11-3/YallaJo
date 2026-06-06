// <copyright file="WeatherPreFetchService.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.BackgroundServices;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using ContentSeo.Infrastructure.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Application.Abstractions.Clock;

/// <summary>
/// Daily worker that pre-fetches weather forecasts for the most-stale-soonest
/// cached coordinates so popular places stay warm without exceeding the
/// upstream daily budget (PDF §11).
///
/// Selection strategy: every day at <see cref="WeatherOptions.PreFetchHourUtc"/>,
/// refresh existing <see cref="WeatherCache"/> rows whose <c>ExpiresAt</c>
/// falls within the next 24h, ordered by soonest-to-expire, capped at
/// <see cref="WeatherOptions.DailyBudget"/> rows. Each refresh is guarded by
/// <see cref="IWeatherBudgetGate"/> so the persistent daily quota is honoured
/// across restarts. No cross-module read is required: places that get queried
/// via the public Weather endpoint stay warm; cold places do not consume budget.
/// </summary>
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
                await RunPrefetchAsync(dailyBudget, stoppingToken).ConfigureAwait(false);
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

    private async Task RunPrefetchAsync(int dailyBudget, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IWeatherProvider>();
        var budgetGate = scope.ServiceProvider.GetRequiredService<IWeatherBudgetGate>();

        if (!provider.IsAvailable)
        {
            logger.LogInformation("WeatherPreFetchService: provider not available, skipping daily pre-fetch.");
            return;
        }

        if (dailyBudget <= 0)
        {
            logger.LogInformation("WeatherPreFetchService: daily budget is zero; skipping pre-fetch.");
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<ContentSeoDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IContentSeoUnitOfWork>();

        var now = clock.UtcNow;
        var refreshWindowEnd = now.AddHours(24);

        // PDF §11: refresh the top-N rows that will expire soonest (within next 24h)
        // so popular coordinates stay warm. Tracked: entity.Refresh() mutates.
        var candidates = await dbContext.Set<WeatherCache>()
            .Where(c => !c.IsDeleted && c.ExpiresAt <= refreshWindowEnd)
            .OrderBy(c => c.ExpiresAt)
            .Take(dailyBudget)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (candidates.Count == 0)
        {
            logger.LogInformation("WeatherPreFetchService: no stale-soonest cache rows found; nothing to pre-fetch.");
            return;
        }

        var ttl = this.options.CacheTtl > TimeSpan.Zero ? this.options.CacheTtl : TimeSpan.FromHours(12);
        var refreshed = 0;
        var budgetSkipped = 0;
        var failures = 0;

        foreach (var entity in candidates)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            if (!await budgetGate.TryConsumeAsync(ct).ConfigureAwait(false))
            {
                budgetSkipped = candidates.Count - refreshed;
                logger.LogInformation(
                    "WeatherPreFetchService: daily budget exhausted after {Refreshed} refreshes; {Remaining} candidates skipped.",
                    refreshed,
                    budgetSkipped);
                break;
            }

            try
            {
                var snapshot = await provider.FetchAsync(entity.RoundedLatitude, entity.RoundedLongitude, ct).ConfigureAwait(false);
                var forecastJson = System.Text.Json.JsonSerializer.Serialize(snapshot.DailyForecasts);
                var fetchedAt = clock.UtcNow;
                var expiresAt = fetchedAt.Add(ttl);

                entity.Refresh(
                    snapshot.Temperature,
                    snapshot.FeelsLike,
                    snapshot.Humidity,
                    snapshot.WindSpeed,
                    snapshot.WindDirection,
                    snapshot.Condition,
                    snapshot.IconCode,
                    snapshot.UvIndex,
                    forecastJson,
                    fetchedAt,
                    expiresAt);

                await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                refreshed++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                failures++;
                logger.LogWarning(
                    ex,
                    "WeatherPreFetchService: concurrent update for ({Lat},{Lng}); skipping.",
                    entity.RoundedLatitude,
                    entity.RoundedLongitude);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                failures++;
                logger.LogWarning(
                    ex,
                    "WeatherPreFetchService: provider call failed for ({Lat},{Lng}); continuing.",
                    entity.RoundedLatitude,
                    entity.RoundedLongitude);
            }
        }

        logger.LogInformation(
            "WeatherPreFetchService: daily run complete. Refreshed={Refreshed}, BudgetExhaustedSkipped={Skipped}, Failures={Failures}, BudgetCap={Budget}, CandidateCount={Candidates}.",
            refreshed,
            budgetSkipped,
            failures,
            dailyBudget,
            candidates.Count);
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
