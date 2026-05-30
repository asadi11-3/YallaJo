using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class TripArcRepository(AnalyticsDbContext context) : EfRepository<TripArc, Guid>(context), ITripArcRepository
{
    public async Task<IReadOnlyList<TripArc>> GetActiveAsync(CancellationToken ct = default)
        => await context.Set<TripArc>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public async Task<TripArc?> GetBestMatchAsync(int days, string? interestTags, CancellationToken ct = default)
    {
        var arcs = await context.Set<TripArc>()
            .Where(x => x.IsActive && x.MinDays <= days && x.MaxDays >= days)
            .ToListAsync(ct);

        if (arcs.Count == 0)
            return await context.Set<TripArc>().Where(x => x.IsActive).OrderBy(x => x.MinDays).FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(interestTags))
            return arcs[0];

        var requestedTags = interestTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToHashSet();

        return arcs
            .OrderByDescending(arc =>
            {
                if (string.IsNullOrWhiteSpace(arc.InterestTags)) return 0;
                var arcTags = arc.InterestTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant());
                return arcTags.Count(requestedTags.Contains);
            })
            .First();
    }
}
