using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetSuggestionBatches;

public sealed class GetSuggestionBatchesQueryHandler(
    ISuggestionBatchRepository batchRepository,
    ILogger<GetSuggestionBatchesQueryHandler> logger) : IQueryHandler<GetSuggestionBatchesQuery, GetSuggestionBatchesResult>
{
    public async Task<Result<GetSuggestionBatchesResult>> Handle(GetSuggestionBatchesQuery request, CancellationToken ct)
    {
        var rows = await batchRepository.GetAllAsync(ct).ConfigureAwait(false);
        var batches = rows
            .Select(batch => new SuggestionBatchDto(batch.Id, batch.SourceKind, batch.SourceId, batch.Context, batch.AlgorithmVersion, batch.ComputedAt, batch.IsStale, batch.ItemCount))
            .ToList();

        logger.LogDebug("Read {Count} analytics suggestion batches", batches.Count);
        return Result<GetSuggestionBatchesResult>.Success(new GetSuggestionBatchesResult(batches));
    }
}
