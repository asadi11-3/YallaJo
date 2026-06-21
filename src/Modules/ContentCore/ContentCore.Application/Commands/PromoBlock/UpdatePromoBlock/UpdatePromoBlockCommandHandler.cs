using ContentCore.Application.Queries.PromoBlock.Common;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.PromoBlock.UpdatePromoBlock;

public sealed class UpdatePromoBlockCommandHandler(
    IPromoBlockRepository promoBlockRepository,
    IContentCoreUnitOfWork unitOfWork,
    ILogger<UpdatePromoBlockCommandHandler> logger)
    : ICommandHandler<UpdatePromoBlockCommand, UpdatePromoBlockResult>
{
    public async Task<Result<UpdatePromoBlockResult>> Handle(
        UpdatePromoBlockCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var promoBlock = await promoBlockRepository.GetByPlacementKeyAsync(
                request.PlacementKey,
                cancellationToken);

            if (promoBlock is null)
            {
                return Result<UpdatePromoBlockResult>.Failure(
                    new Error(
                        "PromoBlock.NotFound",
                        $"Promotional placement '{request.PlacementKey}' was not found."),
                    Outcome.NotFound);
            }

            promoBlock.UpdateContent(
                request.Title,
                request.Description,
                request.ButtonText,
                request.ButtonUrl,
                request.BadgeText,
                request.IconName,
                request.IsActive,
                request.SortOrder,
                request.StartsAt,
                request.EndsAt);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdatePromoBlockResult>.Conflict(
                    new Error(
                        "PromoBlock.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            logger.LogInformation(
                "Promo block updated: {PlacementKey} (Active={IsActive})",
                promoBlock.PlacementKey,
                promoBlock.IsActive);

            return Result<UpdatePromoBlockResult>.Success(
                new UpdatePromoBlockResult(PromoBlockDto.From(promoBlock)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UpdatePromoBlockResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
