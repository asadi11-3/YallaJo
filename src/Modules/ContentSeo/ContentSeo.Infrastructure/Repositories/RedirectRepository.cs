using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRedirectRepository"/>.
/// Compile-only stub for Wave-4 pre-work — chain-flatten / lookup-by-old-url
/// overrides will be added during TASK 3 implementation.
/// </summary>
internal sealed class RedirectRepository(ContentSeoDbContext context)
    : EfRepository<Redirect, Guid>(context), IRedirectRepository
{
    public async Task<Redirect?> GetActiveByOldUrlAsync(string oldUrl, CancellationToken ct = default)
        => await GetAsync(filter: r => r.OldUrl == oldUrl && r.IsActive && !r.IsDeleted,
            asNoTracking:true,
            ct: ct);


    public async Task<IReadOnlyList<Redirect>> GetActivePointingToAsync(string targetUrl, CancellationToken ct = default)
        => await GetAllAsync(filter: r => r.NewUrl == targetUrl && r.IsActive && !r.IsDeleted,
            asNoTracking: false,
            ct:ct);
    
}
