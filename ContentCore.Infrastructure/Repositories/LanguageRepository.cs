using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;
using ContentCore.Infrastructure.Persistence;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// EfRepository uses composition (not inheritance) and does not expose a protected _context.
/// A typed ContentCoreDbContext reference is captured for domain-specific queries.
/// </summary>
internal sealed class LanguageRepository(ContentCoreDbContext context)
    : EfRepository<Language, Guid>(context), ILanguageRepository
{
    private readonly ContentCoreDbContext _db = context;

    public async Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _db.Languages
            .FirstOrDefaultAsync(l => l.Code == code, ct);

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
        => await _db.Languages
            .AnyAsync(l => l.Code == code, ct);
}
