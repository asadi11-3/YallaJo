using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;

namespace Social.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background service that recalculates Bayesian ratings for all entities
/// that have at least one published review. Runs every day at <see cref="RatingRecalculationOptions.TargetTimeUtc"/> UTC.
/// </summary>
/// <remarks>
/// Algorithm (S-R6):
///   VerificationWeight = IsVerifiedBooking ? 1.0 : 0.5
///   RecencyWeight      = age &lt; 90d ? 1.0 : age &lt; 180d ? 0.7 : 0.5
///   EffectiveN         = Σ(VerificationWeight × RecencyWeight) across all published reviews
///   WeightedAvg        = Σ(Rating × w) / EffectiveN
///   BayesianScore      = (EffectiveN × WeightedAvg + C × GlobalAvg) / (EffectiveN + C)
/// </remarks>
internal sealed class RatingRecalculationService(
    IServiceScopeFactory scopeFactory,
    IOptions<RatingRecalculationOptions> options,
    ILogger<RatingRecalculationService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly RatingRecalculationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("[RatingRecalculationService] Service is disabled via configuration.");
            return;
        }

        logger.LogInformation("[RatingRecalculationService] Service started. Daily run at {Time} UTC.",
            _options.TargetTimeUtc);

        var initialDelay = ComputeDelayUntilNext(timeProvider.GetUtcNow().UtcDateTime, _options.TargetTimeUtc);
        logger.LogInformation("[RatingRecalculationService] Next run in {Delay:g}.", initialDelay);

        try
        {
            await Task.Delay(initialDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                await RunRecalculationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("[RatingRecalculationService] Recalculation cancelled mid-run.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[RatingRecalculationService] Unhandled error during recalculation. Will retry tomorrow.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Core recalculation
    // ──────────────────────────────────────────────────────────────────────

    private async Task RunRecalculationAsync(CancellationToken ct)
    {
        logger.LogInformation("[RatingRecalculationService] Starting daily Bayesian rating recalculation.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var reviewRepo = scope.ServiceProvider.GetRequiredService<IReviewRepository>();
        var cacheRepo = scope.ServiceProvider.GetRequiredService<IEntityRatingCacheRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<ISocialUnitOfWork>();

        var targets = await reviewRepo.GetDistinctPublishedTargetsAsync(ct);
        logger.LogInformation("[RatingRecalculationService] Found {Count} distinct targets to process.", targets.Count);

        var updatedCount = 0;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var (targetType, targetId) in targets)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var reviews = await reviewRepo.GetPublishedByTargetAsync(targetType, targetId, ct);
                if (reviews.Count == 0) continue;

                var (avgRating, bayesianScore) = ComputeRatings(reviews, nowUtc, _options);
                var reviewCount = reviews.Count;

                var existing = await cacheRepo.GetByTargetAsync(targetType, targetId, ct);
                if (existing is null)
                {
                    var newCache = EntityRatingCache.Create(targetType, targetId, avgRating, reviewCount, bayesianScore, nowUtc);
                    await cacheRepo.AddAsync(newCache, ct);
                }
                else
                {
                    existing.Update(avgRating, reviewCount, bayesianScore, nowUtc);
                }

                updatedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[RatingRecalculationService] Failed to recalculate rating for {TargetType}:{TargetId}. Skipping.",
                    targetType, targetId);
            }
        }

        if (updatedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "[RatingRecalculationService] Completed. Updated/created {Count} rating caches out of {Total} targets.",
            updatedCount, targets.Count);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Bayesian rating algorithm (S-R6)
    // ──────────────────────────────────────────────────────────────────────

    internal static (decimal AverageRating, decimal BayesianScore) ComputeRatings(
        IReadOnlyList<Review> reviews,
        DateTime nowUtc,
        RatingRecalculationOptions options)
    {
        if (reviews.Count == 0)
            return (0m, options.GlobalAverageRating);

        decimal totalWeightedRating = 0m;
        decimal totalWeight = 0m;
        decimal simpleTotal = 0m;

        foreach (var review in reviews)
        {
            simpleTotal += review.Rating;

            var verificationWeight = review.IsVerifiedBooking ? 1.0m : 0.5m;

            var ageDays = (nowUtc - review.CreatedAt).TotalDays;
            var recencyWeight = ageDays < 90 ? 1.0m
                : ageDays < 180 ? 0.7m
                : 0.5m;

            var weight = verificationWeight * recencyWeight;
            totalWeightedRating += review.Rating * weight;
            totalWeight += weight;
        }

        var simpleAvg = Math.Round(simpleTotal / reviews.Count, 2, MidpointRounding.ToEven);

        // Bayesian: (EffectiveN × WeightedAvg + C × GlobalAvg) / (EffectiveN + C)
        var weightedAvg = totalWeight > 0m ? totalWeightedRating / totalWeight : simpleAvg;
        var c = options.ConfidenceConstant;
        var globalAvg = options.GlobalAverageRating;
        var bayesian = (totalWeight * weightedAvg + c * globalAvg) / (totalWeight + c);

        return (simpleAvg, Math.Round(bayesian, 4, MidpointRounding.ToEven));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Scheduling helper
    // ──────────────────────────────────────────────────────────────────────

    internal static TimeSpan ComputeDelayUntilNext(DateTime utcNow, TimeSpan targetTime)
    {
        var nextRun = utcNow.Date.Add(targetTime);
        if (nextRun <= utcNow) nextRun = nextRun.AddDays(1);
        return nextRun - utcNow;
    }
}
