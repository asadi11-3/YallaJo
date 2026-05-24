using Booking.Application.Caching;
using Booking.Application.Interfaces;
using Booking.Application.Queries.GetRefundPolicyByTour;
using Booking.Domain.Repositories;
using ContentTours.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.RefundPolicy.UpdateRefundPolicy;

public sealed class UpdateRefundPolicyCommandHandler(
    IRefundPolicyRepository refundPolicyRepository,
    ITourOwnershipService tourOwnershipService,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateRefundPolicyCommandHandler> logger)
    : ICommandHandler<UpdateRefundPolicyCommand, RefundPolicyDto>
{
    public async Task<Result<RefundPolicyDto>> Handle(UpdateRefundPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tracked = await refundPolicyRepository
                .GetByIdTrackedAsync(request.PolicyId, cancellationToken)
                .ConfigureAwait(false);
            if (tracked is null)
            {
                return Result.Failure<RefundPolicyDto>(
                    new Error("RefundPolicy.NotFound", "Refund policy was not found."),
                    Outcome.NotFound);
            }

            // Ownership against the stored TourId (never trust body data).
            var auth = await Booking.Application.Commands.RefundPolicy.RefundPolicyOwnershipGuard
                .CheckAsync(tracked.TourId, currentUser, tourOwnershipService, cancellationToken)
                .ConfigureAwait(false);
            if (auth.IsFailure)
            {
                return Result.Failure<RefundPolicyDto>(auth.Errors[0], auth.Outcome);
            }

            if (request.RowVersion is { Length: > 0 })
            {
                refundPolicyRepository
                    .AttachAndMarkModifiedWithConcurrency(
                        tracked,
                        nameof(Booking.Domain.Entities.RefundPolicy.RowVersion),
                        request.RowVersion);
            }

            var tiers = RefundPolicyMapper.ToDomainTiers(request.Tiers).ToList();
            try
            {
                tracked.Update(tiers);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain rejected RefundPolicy update for policy {PolicyId}.", request.PolicyId);
                return Result.Failure<RefundPolicyDto>(
                    new Error("RefundPolicy.InvalidTiers", ex.Message),
                    Outcome.Invalid);
            }

            refundPolicyRepository.Update(tracked);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating RefundPolicy {PolicyId}.", request.PolicyId);
                return Result.Failure<RefundPolicyDto>(
                    new Error("RefundPolicy.ConcurrencyConflict", "Refund policy was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            await cache
                .RemoveByTagAsync(BookingRefundPolicyCacheKeys.TourTag(tracked.TourId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "RefundPolicy {PolicyId} updated for tour {TourId} by user {UserId}.",
                tracked.Id, tracked.TourId, currentUser.UserId);

            return Result.Success(RefundPolicyMapper.ToDto(tracked));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<RefundPolicyDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
