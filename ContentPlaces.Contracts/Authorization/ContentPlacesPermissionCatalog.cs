using YallaJo.SharedKernel.Application.Authorization;

namespace ContentPlaces.Contracts.Authorization;

/// <summary>
/// Permission catalog for the ContentPlaces bounded context.
/// Registered in ContentPlaces.Infrastructure DI — discovered automatically by PermissionSeeder.
/// </summary>
public sealed class ContentPlacesPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "ContentPlaces";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── Place ────────────────────────────────────────────────────────────
        new(ContentPlacesFeatures.Place, AppAction.Read,   PermissionGroup.ContentManagement, "View places"),
        new(ContentPlacesFeatures.Place, AppAction.Create, PermissionGroup.ContentManagement, "Create a place"),
        new(ContentPlacesFeatures.Place, AppAction.Update, PermissionGroup.ContentManagement, "Update place details"),
        // P1 DeleteOwn/DeleteAny split (2026-05-30): replaces Place.Delete.
        new(ContentPlacesFeatures.Place, AppAction.DeleteOwn, PermissionGroup.ContentManagement, "Delete own place"),
        new(ContentPlacesFeatures.Place, AppAction.DeleteAny, PermissionGroup.ContentManagement, "Delete any place (admin)"),
        new(ContentPlacesFeatures.Place, AppAction.SoftDelete, PermissionGroup.ContentManagement, "Soft-delete a place"),
        new(ContentPlacesFeatures.Place, AppAction.Feature,    PermissionGroup.ContentManagement, "Feature/unfeature a place"),

        // ── Business ─────────────────────────────────────────────────────────
        new(ContentPlacesFeatures.Business, AppAction.Read,      PermissionGroup.ContentManagement, "View businesses"),
        new(ContentPlacesFeatures.Business, AppAction.Create,    PermissionGroup.ContentManagement, "Create a business"),
        new(ContentPlacesFeatures.Business, AppAction.Update,    PermissionGroup.ContentManagement, "Update business details"),
        new(ContentPlacesFeatures.Business, AppAction.Delete,    PermissionGroup.ContentManagement, "Delete a business"),
        new(ContentPlacesFeatures.Business, AppAction.Submit,    PermissionGroup.ContentManagement, "Resubmit a rejected business"),
        new(ContentPlacesFeatures.Business, AppAction.Approve,   PermissionGroup.ContentManagement, "Approve a pending business"),
        new(ContentPlacesFeatures.Business, AppAction.Reject,    PermissionGroup.ContentManagement, "Reject a pending business"),
        new(ContentPlacesFeatures.Business, AppAction.Suspend,   PermissionGroup.ContentManagement, "Suspend an approved business"),
        new(ContentPlacesFeatures.Business, AppAction.Reinstate, PermissionGroup.ContentManagement, "Reinstate a suspended business"),

        // ── BusinessHours ────────────────────────────────────────────────────
        new(ContentPlacesFeatures.BusinessHours, AppAction.Update, PermissionGroup.ContentManagement, "Update business operating hours"),

        // ── BusinessStaff ────────────────────────────────────────────────────
        new(ContentPlacesFeatures.BusinessStaff, AppAction.Read,   PermissionGroup.ContentManagement, "View business staff"),
        new(ContentPlacesFeatures.BusinessStaff, AppAction.Create, PermissionGroup.ContentManagement, "Add business staff"),
        new(ContentPlacesFeatures.BusinessStaff, AppAction.Delete, PermissionGroup.ContentManagement, "Remove business staff"),

        // ── BusinessAmenity ──────────────────────────────────────────────────
        new(ContentPlacesFeatures.BusinessAmenity, AppAction.Read,   PermissionGroup.ContentManagement, "View business amenities"),
        new(ContentPlacesFeatures.BusinessAmenity, AppAction.Create, PermissionGroup.ContentManagement, "Add business amenity"),
        new(ContentPlacesFeatures.BusinessAmenity, AppAction.Delete, PermissionGroup.ContentManagement, "Remove business amenity"),

        // ── AccessibilityFeature ─────────────────────────────────────────────
        new(ContentPlacesFeatures.AccessibilityFeature, AppAction.Read,   PermissionGroup.ContentManagement, "View accessibility features"),
        new(ContentPlacesFeatures.AccessibilityFeature, AppAction.Update, PermissionGroup.ContentManagement, "Update accessibility features"),

        // ── ServiceItem ──────────────────────────────────────────────────────
        new(ContentPlacesFeatures.ServiceItem, AppAction.Read,       PermissionGroup.ContentManagement, "View service items"),
        new(ContentPlacesFeatures.ServiceItem, AppAction.Create,     PermissionGroup.ContentManagement, "Create a service item"),
        new(ContentPlacesFeatures.ServiceItem, AppAction.Update,     PermissionGroup.ContentManagement, "Update a service item"),
        new(ContentPlacesFeatures.ServiceItem, AppAction.SoftDelete, PermissionGroup.ContentManagement, "Soft-delete a service item"),

        // ── Booking (place-level booking permission) ─────────────────────────
        new(ContentPlacesFeatures.Booking, AppAction.Read,   PermissionGroup.BookingOperations, "View place bookings"),
        new(ContentPlacesFeatures.Booking, AppAction.Create, PermissionGroup.BookingOperations, "Create a place booking"),
    ];
}
