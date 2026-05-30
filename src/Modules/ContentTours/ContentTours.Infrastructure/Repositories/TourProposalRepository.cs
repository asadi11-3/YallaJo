using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class TourProposalRepository(ContentToursDbContext context)
    : EfRepository<TourProposal, Guid>(context), ITourProposalRepository
{
    public Task<TourProposal?> GetWithDetailsAsync(Guid id, CancellationToken ct = default, bool asNoTracking = true)
    {
        var query = context.TourProposals.Where(p => p.Id == id);
        if (asNoTracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<TourProposal>> GetByGuideIdAsync(Guid tourGuideId, TourProposalStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.TourProposals.Where(p => p.TourGuideId == tourGuideId).AsNoTracking();
        if (statusFilter.HasValue) query = query.Where(p => p.Status == statusFilter.Value);
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TourProposal>> GetPendingAsync(CancellationToken ct = default) =>
        await context.TourProposals
            .Where(p => p.Status == TourProposalStatus.Submitted)
            .OrderBy(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
}
