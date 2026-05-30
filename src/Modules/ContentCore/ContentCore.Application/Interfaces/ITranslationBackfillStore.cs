namespace ContentCore.Application.Interfaces;

public interface ITranslationBackfillStore
{
    Task<IReadOnlyList<TagBackfillCandidate>> FetchNextTagBackfillCandidatesAsync(
        IReadOnlyList<Guid> activeLanguageIds,
        int batchSize,
        CancellationToken ct);

    Task<IReadOnlyList<TranslationLanguagePair>> GetExistingTagTranslationLanguageIdsAsync(
        IReadOnlyList<Guid> tagIds,
        CancellationToken ct);

    void AddTagTranslation(
        Guid tagId,
        Guid languageId,
        string name,
        string slug);

    Task<IReadOnlyList<SpecializationBackfillCandidate>> FetchNextSpecializationBackfillCandidatesAsync(
        IReadOnlyList<Guid> activeLanguageIds,
        int batchSize,
        CancellationToken ct);

    Task<IReadOnlyList<TranslationLanguagePair>> GetExistingSpecializationTranslationLanguageIdsAsync(
        IReadOnlyList<Guid> specializationIds,
        CancellationToken ct);

    void AddSpecializationTranslation(
        Guid specializationId,
        Guid languageId,
        string name,
        string? description);
}

public sealed record TagBackfillCandidate(
    Guid TagId,
    string Name,
    string SourceLanguageCode);

public sealed record SpecializationBackfillCandidate(
    Guid SpecializationId,
    string Name,
    string? Description,
    string SourceLanguageCode);

public sealed record TranslationLanguagePair(Guid EntityId, Guid LanguageId);
