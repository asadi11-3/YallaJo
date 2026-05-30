using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class BoostPackageRepository(AnalyticsDbContext context) : EfRepository<BoostPackage, Guid>(context), IBoostPackageRepository
{
    public async Task<BoostPackage?> GetActiveForEntityAsync(EntityType kind, Guid entityId, CancellationToken ct = default)
        => await context.BoostPackages
            .Where(x => x.EntityKind == kind && x.EntityId == entityId && x.IsActive && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.BoostMultiplier)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BoostPackage>> GetActiveByProviderAsync(Guid providerId, CancellationToken ct = default)
        => await context.BoostPackages
            .Where(x => x.ProviderId == providerId && x.IsActive && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BoostPackage>> GetAllActiveCpcBidsAsync(CancellationToken ct = default)
        => await context.BoostPackages
            .Where(x => x.BillingMode == "CPC" && x.IsActive && x.ExpiresAt > DateTime.UtcNow && x.StartsAt <= DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}
