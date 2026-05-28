using Booking.Application.Interfaces;
using Booking.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.Common;

internal static class AvailabilitySlotOwnership
{
    public const string OwnerMismatchErrorCode = "TourBooking.OwnerMismatch";

    public const string NotAProviderErrorCode = "AvailabilitySlot.NotAProvider";

    public sealed record OwnershipContext(
        BookingTourSnapshot TourSnapshot,
        BookingProviderSnapshot ProviderSnapshot,
        bool IsAdmin);

    public static async Task<Result<OwnershipContext>> ResolveAsync(
        Guid tourId,
        ICurrentUser currentUser,
        IBookingTourSnapshotReader tourSnapshotReader,
        IBookingProviderSnapshotReader providerSnapshotReader,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<OwnershipContext>(
                new Error("TourBooking.Unauthorized", "Authentication is required."),
                Outcome.Unauthorized);
        }

        var tour = await tourSnapshotReader.GetByIdAsync(tourId, cancellationToken).ConfigureAwait(false);
        if (tour is null || !tour.IsActive)
        {
            return Result.Failure<OwnershipContext>(
                new Error("Tour.NotFound", "Tour is unavailable."),
                Outcome.NotFound);
        }

        var provider = await providerSnapshotReader
            .GetByIdAsync(tour.ProviderId, cancellationToken)
            .ConfigureAwait(false);

        if (provider is null)
        {
            return Result.Failure<OwnershipContext>(
                new Error(OwnerMismatchErrorCode, "You are not the provider of this tour."),
                Outcome.Forbidden);
        }

        var isAdmin = currentUser.HasPermission(
            $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}");

        if (!isAdmin && provider.OwnerUserId != currentUser.UserId.Value)
        {
            return Result.Failure<OwnershipContext>(
                new Error(OwnerMismatchErrorCode, "You are not the provider of this tour."),
                Outcome.Forbidden);
        }

        return Result.Success(new OwnershipContext(tour, provider, isAdmin));
    }
}
