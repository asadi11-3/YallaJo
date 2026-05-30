using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class ProviderPaymentMethodRepository(FinanceDbContext context)
    : EfRepository<ProviderPaymentMethod, Guid>(context), IProviderPaymentMethodRepository
{
    private readonly FinanceDbContext _context = context;

    public async Task<IReadOnlyList<ProviderPaymentMethod>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.ProviderPaymentMethods
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderPaymentMethod>> GetByUserIdTrackedAsync(Guid userId, CancellationToken ct = default)
        => await _context.ProviderPaymentMethods
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<ProviderPaymentMethod?> GetDefaultForProviderAsync(Guid userId, CancellationToken ct = default)
        => _context.ProviderPaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault && x.IsVerified, ct);

    public Task<bool> ExistsForProviderAsync(Guid userId, string accountIdentifier, CancellationToken ct = default)
        => _context.ProviderPaymentMethods.AnyAsync(
            x => x.UserId == userId && x.AccountIdentifier == accountIdentifier,
            ct);
}
