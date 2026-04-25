using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class ExternalProviderRepository(AuthDbContext context)
    : EfRepository<ExternalProvider, Guid>(context), IExternalProviderRepository
{
    private readonly AuthDbContext _context = context;

    public Task<ExternalProvider?> FindActiveLinkAsync(
        string normalizedProvider,
        string providerUserId,
        CancellationToken ct = default)
    {
        return _context.ExternalProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ep => ep.Provider == normalizedProvider
                   && ep.ProviderUserId == providerUserId
                   && ep.IsActive,
                ct);
    }
}
