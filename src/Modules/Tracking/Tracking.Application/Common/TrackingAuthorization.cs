using Booking.Contracts.Authorization;
using Security.Contracts.Authorization;
using Tracking.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;

namespace Tracking.Application.Common;

internal static class TrackingAuthorization
{
    private const string TrackingSessionFeature = "TrackingSession";

    public static bool IsAdmin(ICurrentUser currentUser)
        => currentUser.IsInRole(AppRoles.Owner)
            || currentUser.IsInRole(AppRoles.SuperAdmin)
            || currentUser.IsInRole(AppRoles.Admin)
            || currentUser.HasPermission(BuildPermission(AppAction.Manage))
            || currentUser.HasPermission(BuildPermission(AppAction.ReadAny));

    public static async Task<bool> CanManageGuideAsync(
        Guid tourGuideId,
        Guid actorUserId,
        ITourGuideOwnershipService tourGuideOwnershipService,
        CancellationToken cancellationToken)
    {
        var ownership = await tourGuideOwnershipService
            .GetTourGuideOwnershipAsync(tourGuideId, cancellationToken)
            .ConfigureAwait(false);

        return ownership.Exists
            && !ownership.IsDeleted
            && ownership.OwnerUserId == actorUserId;
    }

    public static async Task<bool> CanManageSessionAsync(
        LiveTrackingSession session,
        Guid actorUserId,
        ICurrentUser currentUser,
        ITourGuideOwnershipService tourGuideOwnershipService,
        CancellationToken cancellationToken)
    {
        if (IsAdmin(currentUser))
        {
            return true;
        }

        // Provider-owner write support is intentionally deferred until Tracking gets a
        // provider-ownership contract; for now we safely authorize the assigned guide only.
        return await CanManageGuideAsync(
                session.TourGuideId,
                actorUserId,
                tourGuideOwnershipService,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<bool> CanReadSessionAsync(
        LiveTrackingSession session,
        Guid actorUserId,
        ICurrentUser currentUser,
        ITourGuideOwnershipService tourGuideOwnershipService,
        CancellationToken cancellationToken)
    {
        if (session.UserId == actorUserId || IsAdmin(currentUser))
        {
            return true;
        }

        return await CanManageGuideAsync(
                session.TourGuideId,
                actorUserId,
                tourGuideOwnershipService,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildPermission(string action)
        => $"Permission.{TrackingSessionFeature}.{action}";
}
