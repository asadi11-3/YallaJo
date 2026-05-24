using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetRefundPolicyByTour;

public sealed class GetRefundPolicyByTourQueryHandler(
    IRefundPolicyRepository refundPolicyRepository,
    ILogger<GetRefundPolicyByTourQueryHandler> logger)
    : IQueryHandler<GetRefundPolicyByTourQuery, RefundPolicyDto>
{
    public async Task<Result<RefundPolicyDto>> Handle(GetRefundPolicyByTourQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.TourId == Guid.Empty)
            {
                return Result.Failure<RefundPolicyDto>(
                    new Error("RefundPolicy.InvalidTourId", "TourId is required."),
                    Outcome.Invalid);
            }

            var policy = await refundPolicyRepository
                .GetByTourIdAsync(request.TourId, cancellationToken)
                .ConfigureAwait(false);

            if (policy is null || !policy.IsActive)
            {
                logger.LogDebug(
                    "RefundPolicy lookup for tour {TourId}: returning default tiers (no row or inactive).",
                    request.TourId);
                return Result.Success(RefundPolicyMapper.ToDefaultDto(request.TourId));
            }

            return Result.Success(RefundPolicyMapper.ToDto(policy));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<RefundPolicyDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
