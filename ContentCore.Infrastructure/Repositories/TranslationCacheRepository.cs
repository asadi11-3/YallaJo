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
internal sealed class TranslationCacheRepository(ContentCoreDbContext context)
    : EfRepository<TranslationCache, Guid>(context), ITranslationCacheRepository
{
    private readonly ContentCoreDbContext _db = context;

    public async Task<TranslationCache?> FindCachedAsync(
        string originalText,
        string fromLanguage,
        string toLanguage,
        CancellationToken ct = default)
    {
        var from = fromLanguage.ToLowerInvariant();
        var to = toLanguage.ToLowerInvariant();

        return await _db.TranslationCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.OriginalText == originalText
                     && t.FromLanguage == from
                     && t.ToLanguage == to,
                ct);
    }

    public async Task<IReadOnlyList<TranslationCache>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
        => await _db.TranslationCaches
            .AsNoTracking()
            .Where(t => t.EntityType == entityType && t.EntityId == entityId)
            .OrderBy(t => t.FieldName)
            .ThenBy(t => t.ToLanguage)
            .ToListAsync(ct);
}
