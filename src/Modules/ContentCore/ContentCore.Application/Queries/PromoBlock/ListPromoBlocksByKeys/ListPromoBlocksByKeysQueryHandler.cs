using ContentCore.Application.Queries.PromoBlock.Common;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.PromoBlock.ListPromoBlocksByKeys;

public sealed class ListPromoBlocksByKeysQueryHandler(
    IPromoBlockRepository promoBlockRepository,
    ILogger<ListPromoBlocksByKeysQueryHandler> logger)
    : IQueryHandler<ListPromoBlocksByKeysQuery, IReadOnlyList<PromoBlockDto>>
{
    public async Task<Result<IReadOnlyList<PromoBlockDto>>> Handle(
        ListPromoBlocksByKeysQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PlacementKeys.Count == 0)
            {
                return Result<IReadOnlyList<PromoBlockDto>>.Success(Array.Empty<PromoBlockDto>());
            }

            var blocks = await promoBlockRepository.GetByPlacementKeysAsync(
                request.PlacementKeys,
                cancellationToken);

            var now = DateTimeOffset.UtcNow;

            var visible = blocks
                .Where(b => b.IsVisibleAt(now))
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.PlacementKey, StringComparer.Ordinal)
                .Select(PromoBlockDto.From)
                .ToList();

            return Result<IReadOnlyList<PromoBlockDto>>.Success(visible);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("ListPromoBlocksByKeys cancelled.");
            return Result<IReadOnlyList<PromoBlockDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
