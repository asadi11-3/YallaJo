using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.CreateGuideDiscount;

public sealed class CreateGuideDiscountCommandHandler(
    IGuideDiscountRepository guideDiscountRepository,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<CreateGuideDiscountCommandHandler> logger)
    : ICommandHandler<CreateGuideDiscountCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGuideDiscountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var guideUserId = currentUser.UserId!.Value;
            var utcNow = DateTime.UtcNow;

            var discountResult = GuideDiscount.Create(
                guideUserId,
                request.TourId,
                request.Name,
                request.Description,
                request.DiscountType,
                request.DiscountValue,
                request.Currency,
                request.ValidFrom,
                request.ValidUntil,
                request.MaxUsageCount,
                utcNow);

            if (!discountResult.IsSuccess)
            {
                return Result.Failure<Guid>(discountResult.Error);
            }

            guideDiscountRepository.Add(discountResult.Value!);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "GuideDiscount {DiscountId} created by guide {GuideUserId}.",
                discountResult.Value!.Id, guideUserId);

            return Result.Success(discountResult.Value!.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<Guid>(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
        }
    }
}
