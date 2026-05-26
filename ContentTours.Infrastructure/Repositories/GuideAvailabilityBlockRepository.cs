using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class GuideAvailabilityBlockRepository(ContentToursDbContext context)
    : EfEntityRepository<GuideAvailabilityBlock, Guid>(context), IGuideAvailabilityBlockRepository
{
    public async Task<IReadOnlyList<GuideAvailabilityBlock>> GetByGuideIdAsync(Guid guideId, CancellationToken ct = default)
        => await context.Set<GuideAvailabilityBlock>()
            .Where(x => x.GuideId == guideId)
            .OrderBy(x => x.StartDate)
            .AsNoTracking()
            .ToListAsync(ct);
}
