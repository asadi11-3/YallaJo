using ContentCore.Application.Queries.PromoBlock.Common;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.PromoBlock.ListPromoBlocksAdmin;

public sealed class ListPromoBlocksAdminQueryHandler(
    IPromoBlockRepository promoBlockRepository,
    ILogger<ListPromoBlocksAdminQueryHandler> logger)
    : IQueryHandler<ListPromoBlocksAdminQuery, IReadOnlyList<PromoBlockDto>>
{
    public async Task<Result<IReadOnlyList<PromoBlockDto>>> Handle(
        ListPromoBlocksAdminQuery request,
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

            var ordered = blocks
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.PlacementKey, StringComparer.Ordinal)
                .Select(PromoBlockDto.From)
                .ToList();

            return Result<IReadOnlyList<PromoBlockDto>>.Success(ordered);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("ListPromoBlocksAdmin cancelled.");
            return Result<IReadOnlyList<PromoBlockDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
