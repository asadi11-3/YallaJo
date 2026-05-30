using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

public interface ITranslationCacheRepository : IRepository<TranslationCache, Guid>
{
    /// <summary>
    /// Find a cached translation by original text and language pair.
    /// </summary>
    Task<TranslationCache?> FindCachedAsync( string originalText,
       
        string fromLanguage,
        string toLanguage,
        CancellationToken ct = default);

    /// <summary>
    /// Get all cached translations for a specific entity.
    /// </summary>
    Task<IReadOnlyList<TranslationCache>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Inserts a translation cache entry if an equivalent hash does not already exist.
    /// Uses DB-level locking semantics to reduce race-condition duplicates.
    /// Returns true when inserted, false when an equivalent entry already existed.
    /// </summary>
    Task<bool> TryAddCacheEntryAsync(TranslationCache entry, CancellationToken ct = default);
}
