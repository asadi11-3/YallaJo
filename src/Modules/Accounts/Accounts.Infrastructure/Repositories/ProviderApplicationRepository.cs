using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class ProviderApplicationRepository(AccountsDbContext context)
    : EfRepository<ProviderApplication, Guid>(context), IProviderApplicationRepository
{
    public async Task<ProviderApplication?> GetByIdAsync(Guid applicationId, CancellationToken ct = default)
        => await context.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

    public async Task<ProviderApplication?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.ProviderApplications
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

    public async Task<ProviderApplication?> GetWithDocumentsAsync(Guid applicationId, CancellationToken ct = default)
        => await context.ProviderApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

    public async Task<ProviderApplication?> GetWithDocumentsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.ProviderApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

    public async Task<bool> ExistsApprovedForUserAsync(Guid userId, CancellationToken ct = default)
        => await context.ProviderApplications
            .AnyAsync(a => a.UserId == userId && a.Status == ProviderApplicationStatus.Approved, ct);

    public async Task<IReadOnlyList<ProviderApplication>> GetQueueAsync(
        ProviderApplicationStatus? statusFilter,
        ProviderType? typeFilter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.ProviderApplications.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);

        if (typeFilter.HasValue)
            query = query.Where(a => a.Type == typeFilter.Value);

        return await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetQueueCountAsync(
        ProviderApplicationStatus? statusFilter,
        ProviderType? typeFilter,
        CancellationToken ct = default)
    {
        var query = context.ProviderApplications.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);

        if (typeFilter.HasValue)
            query = query.Where(a => a.Type == typeFilter.Value);

        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<ProviderApplication>> GetApprovedWithExpiringDocumentsAsync(
        DateTime expiryThreshold,
        CancellationToken ct = default)
        => await context.ProviderApplications
            .Include(a => a.Documents)
            .Where(a => a.Status == ProviderApplicationStatus.Approved)
            .Where(a => a.Documents.Any(d => d.ExpiresAt.HasValue && d.ExpiresAt.Value <= expiryThreshold))
            .AsNoTracking()
            .ToListAsync(ct);
}
