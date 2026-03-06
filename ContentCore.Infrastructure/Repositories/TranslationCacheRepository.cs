using ContentCore.Application.Interfaces;
using ContentCore.Domain.Entities;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class TranslationCacheRepository(ContentCoreDbContext context)
    : EfRepository<TranslationCache, Guid>(context), ITranslationCacheRepository
{
    private readonly ContentCoreDbContext _context = context;

    public async Task<TranslationCache?> FindCachedAsync(
        string originalText,
        string fromLanguage,
        string toLanguage,
        CancellationToken ct = default)
    {
        var from = fromLanguage.ToLowerInvariant();
        var to = toLanguage.ToLowerInvariant();

        return await _context.TranslationCaches
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
    {
        return await _context.TranslationCaches
            .AsNoTracking()
            .Where(t => t.EntityType == entityType && t.EntityId == entityId)
            .OrderBy(t => t.FieldName)
            .ThenBy(t => t.ToLanguage)
            .ToListAsync(ct);
    }
}
