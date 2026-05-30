using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class SeasonalityRuleRepository(AnalyticsDbContext context) : EfRepository<SeasonalityRule, Guid>(context), ISeasonalityRuleRepository
{
    public async Task<IReadOnlyList<SeasonalityRule>> GetActiveByPlaceIdAsync(Guid placeId, CancellationToken ct = default)
        => await context.Set<SeasonalityRule>()
            .Where(x => x.PlaceId == placeId && x.IsActive)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SeasonalityRule>> GetAllActiveAsync(CancellationToken ct = default)
        => await context.Set<SeasonalityRule>()
            .Where(x => x.IsActive)
            .ToListAsync(ct);
}
