using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.GetTierProgress;

internal sealed class GetGuideTierProgressQueryHandler(
    ITourGuideRepository guideRepository,
    ILogger<GetGuideTierProgressQueryHandler> logger) : IQueryHandler<GetGuideTierProgressQuery, GuideTierProgressDto>
{
    private static readonly IReadOnlyDictionary<GuideTrustTier, TierThreshold> Thresholds = new Dictionary<GuideTrustTier, TierThreshold>
    {
        [GuideTrustTier.Bronze] = new(CompletedTours: 10, AverageRating: 3.5m, MaxReportRate: 0.15m, ActiveMonths: 2),
        [GuideTrustTier.Silver] = new(CompletedTours: 25, AverageRating: 4.0m, MaxReportRate: 0.10m, ActiveMonths: 6),
        [GuideTrustTier.Gold] = new(CompletedTours: 50, AverageRating: 4.5m, MaxReportRate: 0.05m, ActiveMonths: 12),
    };

    private static readonly IReadOnlyDictionary<GuideTrustTier, decimal> CommissionRates = new Dictionary<GuideTrustTier, decimal>
    {
        [GuideTrustTier.New] = 0.15m,
        [GuideTrustTier.Bronze] = 0.12m,
        [GuideTrustTier.Silver] = 0.10m,
        [GuideTrustTier.Gold] = 0.08m,
    };

    public async Task<Result<GuideTierProgressDto>> Handle(GetGuideTierProgressQuery request, CancellationToken cancellationToken)
    {
        var guide = await guideRepository.GetByUserIdAsync(request.GuideUserId, cancellationToken);
        if (guide is null)
            return Result<GuideTierProgressDto>.Failure(
                new Error("TourGuide.NotFound", "Tour guide not found."), Outcome.NotFound);

        var nextTier = guide.TrustTier switch
        {
            GuideTrustTier.New => GuideTrustTier.Bronze,
            GuideTrustTier.Bronze => GuideTrustTier.Silver,
            GuideTrustTier.Silver => GuideTrustTier.Gold,
            _ => (GuideTrustTier?)null,
        };

        var threshold = nextTier.HasValue
            ? Thresholds[nextTier.Value]
            : new TierThreshold(0, 0m, 0.05m, 0);

        var reportRate = guide.ReviewCount > 0
            ? Math.Round((decimal)guide.ReportCount / guide.ReviewCount, 4)
            : 0m;

        var now = DateTime.UtcNow;
        var activeMonths = Math.Max(0, ((now.Year - guide.CreatedAt.Year) * 12) + now.Month - guide.CreatedAt.Month);
        var currentCommissionRate = CommissionRates.TryGetValue(guide.TrustTier, out var rate)
            ? rate
            : CommissionRates[GuideTrustTier.New];

        logger.LogInformation("Computed tier progress for GuideId={GuideId} CurrentTier={CurrentTier}", guide.Id, guide.TrustTier);

        return Result.Success(new GuideTierProgressDto(
            guide.TrustTier.ToString(),
            nextTier?.ToString(),
            guide.CompletedTourCount,
            threshold.CompletedTours,
            guide.AverageRating,
            threshold.AverageRating,
            reportRate,
            threshold.MaxReportRate,
            activeMonths,
            threshold.ActiveMonths,
            currentCommissionRate));
    }

    private sealed record TierThreshold(int CompletedTours, decimal AverageRating, decimal MaxReportRate, int ActiveMonths);
}
