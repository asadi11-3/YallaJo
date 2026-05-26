using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.DeactivateGuideDiscount;

public sealed class DeactivateGuideDiscountCommandHandler(
    IGuideDiscountRepository guideDiscountRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<DeactivateGuideDiscountCommandHandler> logger)
    : ICommandHandler<DeactivateGuideDiscountCommand>
{
    public async Task<Result> Handle(DeactivateGuideDiscountCommand request, CancellationToken cancellationToken)
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

            var deactivateResult = discount.Deactivate();
            if (!deactivateResult.IsSuccess)
            {
                return deactivateResult;
            }

            guideDiscountRepository.Update(discount);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("GuideDiscount {DiscountId} deactivated by guide {GuideUserId}.", discount.Id, guideUserId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
