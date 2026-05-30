using System.Linq.Expressions;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class ProfileRepository(AccountsDbContext context)
    : EfRepository<Profile, Guid>(context), IProfileRepository
{
    private readonly AccountsDbContext _context = context;

    public Task<Profile?> FirstOrDefaultIncludingDeletedAsync(
        Expression<Func<Profile, bool>> filter,
        bool asNoTracking = false,
        CancellationToken ct = default)
    {
        IQueryable<Profile> query = _context.Set<Profile>()
            .IgnoreQueryFilters();

        if (asNoTracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(filter, ct);
    }
}
