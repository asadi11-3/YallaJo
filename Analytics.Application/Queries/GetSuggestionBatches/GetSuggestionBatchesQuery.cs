using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetSuggestionBatches;

public sealed record GetSuggestionBatchesQuery : IQuery<GetSuggestionBatchesResult>, ICacheableQuery
{
    public string CacheKey => "ct:analytics:admin:batches";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["analytics:batches"];
}

public sealed record GetSuggestionBatchesResult(IReadOnlyList<SuggestionBatchDto> Batches);

public sealed record SuggestionBatchDto(
    Guid Id,
    EntityType SourceKind,
    Guid SourceId,
    SuggestionContext Context,
    string AlgorithmVersion,
    DateTime ComputedAt,
    bool IsStale,
    int ItemCount);
