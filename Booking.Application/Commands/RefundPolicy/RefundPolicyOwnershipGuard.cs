using Booking.Contracts.Authorization;
using ContentTours.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.RefundPolicy;

internal static class RefundPolicyOwnershipGuard
{
    public const string ErrorCodeUnauthorized = "RefundPolicy.Unauthorized";
    public const string ErrorCodeTourNotFound = "RefundPolicy.TourNotFound";
    public const string ErrorCodeOwnerMismatch = "RefundPolicy.OwnerMismatch";
    public const string ErrorCodeOwnershipUnsupported = "RefundPolicy.OwnershipUnsupported";

    public static async Task<Result<Unit>> CheckAsync(
        Guid tourId,
        ICurrentUser currentUser,
        ITourOwnershipService tourOwnershipService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(tourOwnershipService);

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<Unit>(
                new Error(ErrorCodeUnauthorized, "Authentication is required."),
                Outcome.Unauthorized);
        }

        var ownership = await tourOwnershipService
            .GetTourOwnershipAsync(tourId, cancellationToken)
            .ConfigureAwait(false);

        if (!ownership.IsSupported)
        {
            return Result.Failure<Unit>(
                new Error(
                    ErrorCodeOwnershipUnsupported,
                    "Tour ownership lookup is not available; cannot authorize mutation."),
                Outcome.ServerError);
        }

        if (!ownership.Exists || ownership.IsDeleted)
        {
            return Result.Failure<Unit>(
                new Error(ErrorCodeTourNotFound, "Tour was not found."),
                Outcome.NotFound);
        }

        var isAdmin = currentUser.HasPermission(
            $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}");

        if (!isAdmin && ownership.OwnerUserId != currentUser.UserId.Value)
        {
            return Result.Failure<Unit>(
                new Error(ErrorCodeOwnerMismatch, "You are not authorized to manage this tour's refund policy."),
                Outcome.Forbidden);
        }

        return Result.Success(Unit.Value);
    }

    public readonly record struct Unit
    {
        public static Unit Value { get; } = default;
    }
}
