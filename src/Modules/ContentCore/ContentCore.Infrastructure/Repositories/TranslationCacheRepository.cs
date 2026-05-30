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
        var hash = TranslationCache.ComputeHash(originalText, from, to);

        return await _db.TranslationCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.OriginalTextHash == hash
                     && t.OriginalText == originalText
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

    public async Task<bool> TryAddCacheEntryAsync(TranslationCache entry, CancellationToken ct = default)
    {
        // Atomic insert-if-not-exists by hash with UPDLOCK/HOLDLOCK to reduce race duplicates.
        // This write is intentionally immediate (not staged in UoW) because translation cache is
        // auxiliary and should not fail the caller's primary business transaction.
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO [content_core].[TranslationCaches]
    ([Id],[OriginalText],[OriginalTextHash],[TranslatedText],[FromLanguage],[ToLanguage],[Confidence],[Status],[EntityType],[EntityId],[FieldName],[CreatedAt],[UpdatedAt])
SELECT
    {entry.Id}, {entry.OriginalText}, {entry.OriginalTextHash}, {entry.TranslatedText}, {entry.FromLanguage}, {entry.ToLanguage},
    {entry.Confidence}, {(byte)entry.Status}, {entry.EntityType}, {entry.EntityId}, {entry.FieldName}, {entry.CreatedAt}, {entry.UpdatedAt}
WHERE NOT EXISTS (
    SELECT 1
    FROM [content_core].[TranslationCaches] WITH (UPDLOCK, HOLDLOCK)
    WHERE [OriginalTextHash] = {entry.OriginalTextHash}
);", ct);

        return rows > 0;
    }
}
