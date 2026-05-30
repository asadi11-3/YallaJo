using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

/// <summary>
/// EF-backed implementation of <see cref="IProviderBankAccountRepository"/>.
/// </summary>
/// <remarks>
/// The legacy schema stores the provider identity in <c>UserId</c>; T4 will rename this column to <c>ProviderId</c>.
/// Until then, this repository filters by <c>UserId</c> while exposing the post-T4 method names.
/// </remarks>
internal sealed class ProviderBankAccountRepository(FinanceDbContext context)
    : EfRepository<ProviderBankAccount, Guid>(context), IProviderBankAccountRepository
{
    private readonly FinanceDbContext _context = context;

    public Task<ProviderBankAccount?> GetDefaultForProviderAsync(
        Guid providerId,
        string currency,
        CancellationToken ct = default)
        => _context.ProviderBankAccounts
            .AsNoTracking()
            .Where(a => a.UserId == providerId
                && a.Currency == currency
                && a.IsVerified
                && a.IsDefault)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProviderBankAccount>> GetVerifiedForProviderAsync(
        Guid providerId,
        string currency,
        CancellationToken ct = default)
        => await _context.ProviderBankAccounts
            .AsNoTracking()
            .Where(a => a.UserId == providerId
                && a.Currency == currency
                && a.IsVerified)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(ct);
}
