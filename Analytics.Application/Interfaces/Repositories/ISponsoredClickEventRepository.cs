using Analytics.Domain.Entities;

namespace Analytics.Application.Interfaces.Repositories;

public interface ISponsoredClickEventRepository
{
    Task AddAsync(SponsoredClickEvent entity, CancellationToken ct = default);
    Task<bool> HasChargedTodayAsync(Guid bidId, Guid? userId, string? sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<SponsoredClickEvent>> GetByBidIdAsync(Guid bidId, CancellationToken ct = default);
}
