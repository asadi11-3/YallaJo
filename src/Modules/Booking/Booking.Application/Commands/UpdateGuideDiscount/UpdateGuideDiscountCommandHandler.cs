using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.UpdateGuideDiscount;

public sealed class UpdateGuideDiscountCommandHandler(
    IGuideDiscountRepository guideDiscountRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<UpdateGuideDiscountCommandHandler> logger)
    : ICommandHandler<UpdateGuideDiscountCommand>
{
    public async Task<Result> Handle(UpdateGuideDiscountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var guideUserId = currentUser.UserId!.Value;

            var discount = await guideDiscountRepository
                .GetByIdAsync(request.DiscountId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (discount is null)
            {
                return Result.Failure(new Error("GuideDiscount.NotFound", "Discount not found."), Outcome.NotFound);
            }

            if (discount.GuideUserId != guideUserId)
            {
                return Result.Failure(new Error("GuideDiscount.Unauthorized", "You do not own this discount."), Outcome.Forbidden);
            }

            var updateResult = discount.Update(
                request.Name,
                request.Description,
                request.DiscountValue,
                request.ValidFrom,
                request.ValidUntil,
                request.MaxUsageCount);

            if (!updateResult.IsSuccess)
            {
                return updateResult;
            }

            guideDiscountRepository.Update(discount);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("GuideDiscount.ConcurrencyConflict", "The discount was modified concurrently. Reload and retry."),
                    Outcome.Conflict);
            }

            logger.LogInformation("GuideDiscount {DiscountId} updated by guide {GuideUserId}.", discount.Id, guideUserId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
