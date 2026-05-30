using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface ITripArcRepository : IRepository<TripArc, Guid>
{
    Task<IReadOnlyList<TripArc>> GetActiveAsync(CancellationToken ct = default);
    Task<TripArc?> GetBestMatchAsync(int days, string? interestTags, CancellationToken ct = default);
}
