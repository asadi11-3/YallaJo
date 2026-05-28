using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface ITourProposalRepository : IRepository<TourProposal, Guid>
{
    Task<TourProposal?> GetWithDetailsAsync(Guid id, CancellationToken ct = default, bool asNoTracking = true);

    Task<IReadOnlyList<TourProposal>> GetByGuideIdAsync(Guid tourGuideId, TourProposalStatus? statusFilter = null, CancellationToken ct = default);

    Task<IReadOnlyList<TourProposal>> GetPendingAsync(CancellationToken ct = default);
}
