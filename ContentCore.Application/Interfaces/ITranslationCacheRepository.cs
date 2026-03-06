using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Application.Interfaces;

public interface ITranslationCacheRepository : IRepository<TranslationCache, Guid>
{
    /// <summary>
    /// Find a cached translation by original text and language pair.
    /// </summary>
    Task<TranslationCache?> FindCachedAsync(
        string originalText,
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
}
