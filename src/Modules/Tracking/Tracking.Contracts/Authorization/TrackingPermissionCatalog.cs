using YallaJo.SharedKernel.Application.Authorization;

namespace Tracking.Contracts.Authorization;


public sealed class TrackingPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Tracking";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        
        new(TrackingFeatures.TrackingSession, AppAction.Create,  PermissionGroup.BookingOperations, "Start a live tracking session"),

        new(TrackingFeatures.TrackingSession, AppAction.Update,  PermissionGroup.BookingOperations, "Update a live tracking session (add location, pause, resume, end, checkpoints)"),

        new(TrackingFeatures.TrackingSession, AppAction.ReadOwn, PermissionGroup.BookingOperations, "View own booking's live tracking session"),

        new(TrackingFeatures.TrackingSession, AppAction.ReadAny, PermissionGroup.BookingOperations, "View any live tracking session (admin)"),

        new(TrackingFeatures.TrackingSession, AppAction.Manage,  PermissionGroup.BookingOperations, "Admin: manage any live tracking session (force-end, expire)"),
    ];
}
