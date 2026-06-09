using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Analytics.Infrastructure.Persistence.Seeding;

internal sealed class AnalyticsDbInitializer(AnalyticsDbContext dbContext) : IModuleDbInitializer
{
    public int Order => 90; // after ContentToursDbInitializer (80)

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await EnsurePopularityScoresAsync(ct);
    }

    /// <summary>
    /// Seeds PopularityScore rows for all seed content entities.
    /// Each score is initialized and immediately recalculated so the popular-*
    /// API endpoints return results even on a fresh DB (they filter IsStale=false).
    /// Domain events are cleared to avoid outbox noise during seeding.
    /// </summary>
    private async Task EnsurePopularityScoresAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var seeds = new (EntityType Type, Guid Id, decimal Score, int Interactions)[]
        {
            // ── Places ────────────────────────────────────────────────────
            (EntityType.Place, SeedContentIds.PlacePetra,   920m, 2400),
            (EntityType.Place, SeedContentIds.PlaceJerash,  740m, 1600),
            (EntityType.Place, SeedContentIds.PlaceDeadSea, 880m, 3100),
            (EntityType.Place, SeedContentIds.PlaceWadiRum, 960m, 3800),
            (EntityType.Place, SeedContentIds.PlaceAqaba,   670m, 1200),
            (EntityType.Place, SeedContentIds.PlaceAmman,   590m,  980),

            // ── Businesses ────────────────────────────────────────────────
            (EntityType.Business, SeedContentIds.BusinessPetraGuides,   810m, 1800),
            (EntityType.Business, SeedContentIds.BusinessAmmanFood,     640m, 1100),
            (EntityType.Business, SeedContentIds.BusinessDeadSeaResort, 900m, 2700),
            (EntityType.Business, SeedContentIds.BusinessWadiRumCamp,   980m, 4200),
            (EntityType.Business, SeedContentIds.BusinessAqabaDivers,   720m, 1500),

            // ── Tours ─────────────────────────────────────────────────────
            (EntityType.Tour, SeedContentIds.TourPetraExplorer, 850m, 2200),
            (EntityType.Tour, SeedContentIds.TourDeadSeaDay,    790m, 1900),
            (EntityType.Tour, SeedContentIds.TourWadiRumCamp,   940m, 3500),
            (EntityType.Tour, SeedContentIds.TourAmmanCity,     660m, 1300),
        };

        foreach (var (type, id, score, interactions) in seeds)
        {
            var exists = await dbContext.PopularityScores
                .AnyAsync(x => x.EntityType == type && x.EntityId == id, ct);

            if (exists)
                continue;

            var popularityScore = PopularityScore.Initialize(type, id, now);
            popularityScore.ClearDomainEvents(); // suppress PopularityScoreInitializedDomainEvent

            popularityScore.Recalculate(score, interactions, now);
            popularityScore.ClearDomainEvents(); // suppress PopularityScoreRecalculatedDomainEvent

            dbContext.PopularityScores.Add(popularityScore);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
