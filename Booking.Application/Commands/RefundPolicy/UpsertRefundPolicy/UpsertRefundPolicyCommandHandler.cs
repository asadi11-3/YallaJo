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

namespace Booking.Application.Commands.RefundPolicy.UpsertRefundPolicy;

public sealed class UpsertRefundPolicyCommandHandler(
    IRefundPolicyRepository refundPolicyRepository,
    ITourOwnershipService tourOwnershipService,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpsertRefundPolicyCommandHandler> logger)
    : ICommandHandler<UpsertRefundPolicyCommand, RefundPolicyDto>
{
    public async Task<Result<RefundPolicyDto>> Handle(UpsertRefundPolicyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var auth = await RefundPolicyOwnershipGuard
                .CheckAsync(request.TourId, currentUser, tourOwnershipService, cancellationToken)
                .ConfigureAwait(false);
            if (auth.IsFailure)
            {
                return Result.Failure<RefundPolicyDto>(auth.Errors[0], auth.Outcome);
            }

            var existing = await refundPolicyRepository
                .GetByTourIdAsync(request.TourId, cancellationToken)
                .ConfigureAwait(false);

            var tiers = RefundPolicyMapper.ToDomainTiers(request.Tiers).ToList();
            bool created;

            if (existing is null)
            {
                Booking.Domain.Entities.RefundPolicy policy;
                try
                {
                    policy = Booking.Domain.Entities.RefundPolicy.Create(request.TourId, tiers);
                }
                catch (BusinessRuleViolationException ex)
                {
                    logger.LogWarning(ex, "Domain rejected new RefundPolicy for tour {TourId}.", request.TourId);
                    return Result.Failure<RefundPolicyDto>(
                        new Error("RefundPolicy.InvalidTiers", ex.Message),
                        Outcome.Invalid);
                }

                await refundPolicyRepository.AddAsync(policy, cancellationToken).ConfigureAwait(false);
                created = true;

                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateException ex)
                {
                    logger.LogWarning(ex, "DbUpdateException on RefundPolicy insert for tour {TourId}; retrying as update.", request.TourId);
                    return Result.Failure<RefundPolicyDto>(
                        new Error("RefundPolicy.UpsertRaceConflict", "Another request modified this tour's refund policy concurrently. Please retry."),
                        Outcome.Conflict);
                }

                await InvalidateCacheAsync(policy.TourId, cancellationToken).ConfigureAwait(false);
                logger.LogInformation(
                    "RefundPolicy created for tour {TourId} (policy {PolicyId}) by user {UserId}.",
                    request.TourId, policy.Id, currentUser.UserId);
                return Result.Created(RefundPolicyMapper.ToDto(policy));
            }

            var tracked = await refundPolicyRepository
                .GetByIdTrackedAsync(existing.Id, cancellationToken)
                .ConfigureAwait(false);
            if (tracked is null)
            {
                return Result.Failure<RefundPolicyDto>(
                    new Error("RefundPolicy.NotFound", "Refund policy was removed concurrently."),
                    Outcome.Conflict);
            }

            try
            {
                tracked.Update(tiers);
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogWarning(ex, "Domain rejected RefundPolicy update for tour {TourId}.", request.TourId);
                return Result.Failure<RefundPolicyDto>(
                    new Error("RefundPolicy.InvalidTiers", ex.Message),
                    Outcome.Invalid);
            }

            refundPolicyRepository.Update(tracked);
            created = false;

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating RefundPolicy for tour {TourId}.", request.TourId);
                return Result.Failure<RefundPolicyDto>(
                    new Error("RefundPolicy.ConcurrencyConflict", "Refund policy was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            await InvalidateCacheAsync(tracked.TourId, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "RefundPolicy updated for tour {TourId} (policy {PolicyId}) by user {UserId}; created={Created}.",
                request.TourId, tracked.Id, currentUser.UserId, created);
            return Result.Success(RefundPolicyMapper.ToDto(tracked));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<RefundPolicyDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }

    private async Task InvalidateCacheAsync(Guid tourId, CancellationToken ct)
        => await cache.RemoveByTagAsync(BookingRefundPolicyCacheKeys.TourTag(tourId), ct).ConfigureAwait(false);
}
