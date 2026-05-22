using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentBlogs.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background service (02:00 UTC) that evaluates tier-promotion eligibility
/// and auto-demotion for content creators.
/// <list type="bullet">
///   <item>Pass 1 — Tier 0 → Tier 1 eligibility (5+ published, ReportRate &lt; 5%)</item>
///   <item>Pass 2 — Tier 1 → Tier 2 eligibility (25+ published, ReportRate &lt; 2%, reactions ≥ 500)</item>
///   <item>Pass 3 — Auto-demotion (ReportRate &gt; 15% sustained)</item>
/// </list>
/// Processes in batches of 200 to avoid long-running transactions.
/// </summary>
public sealed class CreatorTierPromotionService(
    IServiceScopeFactory scopeFactory,
    ILogger<CreatorTierPromotionService> logger) : BackgroundService
{
    private const int BatchSize = 200;
    private const double AutoDemotionReportRateThreshold = 0.15;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPromotionPassesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Creator tier promotion job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunPromotionPassesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var profileRepo = scope.ServiceProvider.GetRequiredService<ICreatorProfileRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IContentBlogsUnitOfWork>();

        // ── Pass 1: Tier 0 → Tier 1 eligibility ────────────────────────────
        var tier0Count = await MarkEligibleTier0Async(profileRepo, unitOfWork, ct);
        if (tier0Count > 0)
        {
            logger.LogInformation(
                "Tier promotion: marked {Count} Tier-0 creator(s) eligible for Tier-1",
                tier0Count);
        }

        // ── Pass 2: Tier 1 → Tier 2 eligibility ────────────────────────────
        var tier1Count = await MarkEligibleTier1Async(profileRepo, unitOfWork, ct);
        if (tier1Count > 0)
        {
            logger.LogInformation(
                "Tier promotion: marked {Count} Tier-1 creator(s) eligible for Tier-2",
                tier1Count);
        }

        // ── Pass 3: Auto-demotion ──────────────────────────────────────────
        var demotedCount = await AutoDemoteAsync(profileRepo, unitOfWork, ct);
        if (demotedCount > 0)
        {
            logger.LogInformation(
                "Tier promotion: auto-demoted {Count} creator(s) for high report rate",
                demotedCount);
        }
    }

    private static async Task<int> MarkEligibleTier0Async(
        ICreatorProfileRepository profileRepo,
        IContentBlogsUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var total = 0;

        while (true)
        {
            var batch = await profileRepo.GetTier0PromotionCandidatesAsync(BatchSize, ct);
            if (batch.Count == 0)
                break;

            foreach (var profile in batch)
            {
                profile.MarkEligibleForTier1();
            }

            await unitOfWork.SaveChangesAsync(ct);
            total += batch.Count;

            if (batch.Count < BatchSize)
                break;
        }

        return total;
    }

    private static async Task<int> MarkEligibleTier1Async(
        ICreatorProfileRepository profileRepo,
        IContentBlogsUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var total = 0;

        while (true)
        {
            var batch = await profileRepo.GetTier1PromotionCandidatesAsync(BatchSize, ct);
            if (batch.Count == 0)
                break;

            foreach (var profile in batch)
            {
                profile.MarkEligibleForTier2();
            }

            await unitOfWork.SaveChangesAsync(ct);
            total += batch.Count;

            if (batch.Count < BatchSize)
                break;
        }

        return total;
    }

    private static async Task<int> AutoDemoteAsync(
        ICreatorProfileRepository profileRepo,
        IContentBlogsUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var total = 0;
        var utcNow = DateTime.UtcNow;

        while (true)
        {
            var batch = await profileRepo.GetAutoDemotionCandidatesAsync(
                AutoDemotionReportRateThreshold, BatchSize, ct);
            if (batch.Count == 0)
                break;

            foreach (var profile in batch)
            {
                // Demote one tier down
                var targetTier = profile.TrustTier switch
                {
                    CreatorTrustTier.Expert => CreatorTrustTier.Trusted,
                    CreatorTrustTier.Trusted => CreatorTrustTier.New,
                    _ => CreatorTrustTier.New
                };

                profile.Demote(
                    targetTier,
                    $"Auto-demotion: report rate {profile.ReportRate:P1} exceeds threshold",
                    utcNow);
            }

            await unitOfWork.SaveChangesAsync(ct);
            total += batch.Count;

            if (batch.Count < BatchSize)
                break;
        }

        return total;
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(2); // 02:00 UTC
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}
