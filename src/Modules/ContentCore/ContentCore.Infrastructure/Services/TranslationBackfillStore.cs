using ContentCore.Application.Interfaces;
using ContentCore.Domain.Entities;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Services;

public sealed class TranslationBackfillStore(ContentCoreDbContext dbContext)
    : ITranslationBackfillStore
{
    // ── Tag ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TagBackfillCandidate>> FetchNextTagBackfillCandidatesAsync(
        IReadOnlyList<Guid> activeLanguageIds,
        int batchSize,
        CancellationToken ct)
    {
        if (activeLanguageIds.Count == 0)
            return [];
        var activeIds = activeLanguageIds as IList<Guid> ?? activeLanguageIds.ToList();
        var activeCount = activeIds.Count;

        var rows = await dbContext.Tags
            .AsNoTracking()
            .Where(t => dbContext.TagTranslations
                .Count(tt => tt.TagId == t.Id && activeIds.Contains(tt.LanguageId)) < activeCount)
            .OrderBy(t => t.Id)
            .Take(batchSize)
            .Select(t => new TagBackfillCandidate(t.Id, t.Name, t.SourceLanguageCode))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows;
    }

    public async Task<IReadOnlyList<TranslationLanguagePair>> GetExistingTagTranslationLanguageIdsAsync(
        IReadOnlyList<Guid> tagIds,
        CancellationToken ct)
    {
        if (tagIds.Count == 0)
            return [];

        var ids = tagIds as IList<Guid> ?? tagIds.ToList();

        var rows = await dbContext.TagTranslations
            .AsNoTracking()
            .Where(tt => ids.Contains(tt.TagId))
            .Select(tt => new TranslationLanguagePair(tt.TagId, tt.LanguageId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows;
    }

    public void AddTagTranslation(
        Guid tagId,
        Guid languageId,
        string name,
        string slug)
    {
        var translation = TagTranslation.Create(tagId, languageId, name, slug);
        dbContext.TagTranslations.Add(translation);
    }


    public async Task<IReadOnlyList<SpecializationBackfillCandidate>> FetchNextSpecializationBackfillCandidatesAsync(
        IReadOnlyList<Guid> activeLanguageIds,
        int batchSize,
        CancellationToken ct)
    {
        if (activeLanguageIds.Count == 0)
            return [];

        // Same COUNT-comparison anti-join as FetchNextTagBackfillCandidatesAsync.
        var activeIds = activeLanguageIds as IList<Guid> ?? activeLanguageIds.ToList();
        var activeCount = activeIds.Count;

        var rows = await dbContext.Specializations
            .AsNoTracking()
            .Where(s => dbContext.SpecializationTranslations
                .Count(st => st.SpecializationId == s.Id && activeIds.Contains(st.LanguageId)) < activeCount)
            .OrderBy(s => s.Id)
            .Take(batchSize)
            .Select(s => new SpecializationBackfillCandidate(
                s.Id, s.Name, s.Description, s.SourceLanguageCode))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows;
    }

    public async Task<IReadOnlyList<TranslationLanguagePair>> GetExistingSpecializationTranslationLanguageIdsAsync(
        IReadOnlyList<Guid> specializationIds,
        CancellationToken ct)
    {
        if (specializationIds.Count == 0)
            return [];

        var ids = specializationIds as IList<Guid> ?? specializationIds.ToList();

        var rows = await dbContext.SpecializationTranslations
            .AsNoTracking()
            .Where(st => ids.Contains(st.SpecializationId))
            .Select(st => new TranslationLanguagePair(st.SpecializationId, st.LanguageId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows;
    }

    public void AddSpecializationTranslation(
        Guid specializationId,
        Guid languageId,
        string name,
        string? description)
    {
        var translation = SpecializationTranslation.Create(
            specializationId, languageId, name, description);
        dbContext.SpecializationTranslations.Add(translation);
    }
}
