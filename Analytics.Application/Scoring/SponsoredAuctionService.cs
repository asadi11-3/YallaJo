using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Scoring;

/// <summary>
/// Runs a Vickrey (second-price) auction for CPC sponsored placements.
/// Quality-adjusted bid = bidPerClick × baseQualityScore × (1 + log10(providerCTR + 0.01))
/// Winner pays second-highest quality-adjusted bid.
/// </summary>
public sealed class SponsoredAuctionService(
    IBoostPackageRepository boostRepo,
    IEntityAttributeSnapshotRepository snapshotRepo)
{
    public async Task<IReadOnlyList<AuctionWinner>> RunAuctionAsync(
        SuggestionContext context,
        EntityType sourceKind,
        Guid sourceId,
        int maxSlots,
        CancellationToken ct)
    {
        // Get all active CPC bids with remaining budget
        var allCpcBids = await boostRepo.GetAllActiveCpcBidsAsync(ct);
        var cpcBids = allCpcBids
            .Where(b => b.HasBudgetRemaining())
            .ToList();

        if (cpcBids.Count == 0)
            return [];

        // Compute quality-adjusted bids
        var scoredBids = new List<(BoostPackage Bid, decimal QualityAdjustedBid, decimal BaseQuality)>();

        foreach (var bid in cpcBids)
        {
            var snapshot = await snapshotRepo.GetByEntityAsync(bid.EntityKind, bid.EntityId, ct);
            if (snapshot is null || snapshot.IsDeleted) continue;

            var baseQuality = Math.Max(0.1m, snapshot.AverageRating / 5m);
            var providerCtr = 0.05m; // Default CTR estimate; replaced by real data in metrics aggregation
            var qualityAdjustedBid = (bid.BidPerClick ?? 0m) * baseQuality * (1m + (decimal)Math.Log10((double)(providerCtr + 0.01m)));

            scoredBids.Add((bid, qualityAdjustedBid, baseQuality));
        }

        // Sort descending by quality-adjusted bid
        scoredBids.Sort((a, b) => b.QualityAdjustedBid.CompareTo(a.QualityAdjustedBid));

        // Vickrey: winner pays second-highest price
        var winners = new List<AuctionWinner>();
        for (var i = 0; i < Math.Min(maxSlots, scoredBids.Count); i++)
        {
            var secondPrice = i + 1 < scoredBids.Count
                ? scoredBids[i + 1].QualityAdjustedBid
                : scoredBids[i].QualityAdjustedBid * 0.5m; // Floor at 50% if no second bidder

            winners.Add(new AuctionWinner(
                scoredBids[i].Bid,
                scoredBids[i].QualityAdjustedBid,
                secondPrice,
                scoredBids[i].BaseQuality));
        }

        return winners;
    }
}

public sealed record AuctionWinner(
    BoostPackage Bid,
    decimal QualityAdjustedBid,
    decimal ChargePerClick,
    decimal BaseQuality);
