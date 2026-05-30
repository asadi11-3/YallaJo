using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Repositories;

public sealed class SponsoredClickEventRepository(AnalyticsDbContext context) : ISponsoredClickEventRepository
{
    public async Task AddAsync(SponsoredClickEvent entity, CancellationToken ct = default)
    {
        await context.Set<SponsoredClickEvent>().AddAsync(entity, ct);
    }

    public async Task<bool> HasChargedTodayAsync(Guid bidId, Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await context.Set<SponsoredClickEvent>()
            .AnyAsync(e => e.BidId == bidId
                && !e.IsFraudulent
                && e.ClickedAt >= today
                && ((userId.HasValue && e.UserId == userId) || (sessionId != null && e.SessionId == sessionId)),
                ct);
    }

    public async Task<IReadOnlyList<SponsoredClickEvent>> GetByBidIdAsync(Guid bidId, CancellationToken ct = default)
        => await context.Set<SponsoredClickEvent>()
            .Where(e => e.BidId == bidId)
            .OrderByDescending(e => e.ClickedAt)
            .ToListAsync(ct);
}
