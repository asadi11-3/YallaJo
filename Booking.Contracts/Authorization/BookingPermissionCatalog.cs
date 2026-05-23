using YallaJo.SharedKernel.Application.Authorization;

namespace Booking.Contracts.Authorization;

/// <summary>
/// Permission catalog for the Booking bounded context.
/// Registered in Booking.Infrastructure DI — discovered automatically by PermissionSeeder.
/// </summary>
public sealed class BookingPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Booking";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── TourBooking (7) ─────────────────────────────────────────────────
        new(BookingFeatures.TourBooking, AppAction.ReadOwn,  PermissionGroup.BookingOperations, "View own bookings"),
        new(BookingFeatures.TourBooking, AppAction.ReadAny,  PermissionGroup.BookingOperations, "View any booking (admin)"),
        new(BookingFeatures.TourBooking, AppAction.Create,   PermissionGroup.BookingOperations, "Create a booking"),
        new(BookingFeatures.TourBooking, AppAction.Cancel,   PermissionGroup.BookingOperations, "Cancel a booking"),
        new(BookingFeatures.TourBooking, AppAction.Confirm,  PermissionGroup.BookingOperations, "Confirm a booking"),
        new(BookingFeatures.TourBooking, AppAction.Reject,   PermissionGroup.BookingOperations, "Reject a booking"),
        new(BookingFeatures.TourBooking, AppAction.Complete, PermissionGroup.BookingOperations, "Mark a booking as completed"),

        // ── AvailabilitySlot (4) ────────────────────────────────────────────
        new(BookingFeatures.AvailabilitySlot, AppAction.Read,   PermissionGroup.BookingOperations, "View availability slots"),
        new(BookingFeatures.AvailabilitySlot, AppAction.Create, PermissionGroup.BookingOperations, "Create availability slot"),
        new(BookingFeatures.AvailabilitySlot, AppAction.Update, PermissionGroup.BookingOperations, "Update availability slot"),
        new(BookingFeatures.AvailabilitySlot, AppAction.Delete, PermissionGroup.BookingOperations, "Delete availability slot"),

        // ── RefundPolicy (3) ────────────────────────────────────────────────
        new(BookingFeatures.RefundPolicy, AppAction.Read,   PermissionGroup.BookingOperations, "View refund policies"),
        new(BookingFeatures.RefundPolicy, AppAction.Create, PermissionGroup.BookingOperations, "Create refund policy"),
        new(BookingFeatures.RefundPolicy, AppAction.Update, PermissionGroup.BookingOperations, "Update refund policy"),

        // ── JoinRequest (4) ─────────────────────────────────────────────────
        new(BookingFeatures.JoinRequest, AppAction.Create,  PermissionGroup.BookingOperations, "Submit join request"),
        new(BookingFeatures.JoinRequest, AppAction.ReadOwn, PermissionGroup.BookingOperations, "View own join requests"),
        new(BookingFeatures.JoinRequest, AppAction.Approve, PermissionGroup.BookingOperations, "Approve join request"),
        new(BookingFeatures.JoinRequest, AppAction.Reject,  PermissionGroup.BookingOperations, "Reject join request"),

        // ── ProviderDocument (5) ────────────────────────────────────────────
        new(BookingFeatures.ProviderDocument, AppAction.Read,    PermissionGroup.BookingOperations, "View provider documents"),
        new(BookingFeatures.ProviderDocument, AppAction.Create,  PermissionGroup.BookingOperations, "Upload provider document"),
        new(BookingFeatures.ProviderDocument, AppAction.Update,  PermissionGroup.BookingOperations, "Update provider document (replace file or expiry)"),
        new(BookingFeatures.ProviderDocument, AppAction.Approve, PermissionGroup.BookingOperations, "Approve provider document"),
        new(BookingFeatures.ProviderDocument, AppAction.Reject,  PermissionGroup.BookingOperations, "Reject provider document"),

        // ── SlotLock (2) ────────────────────────────────────────────────────
        new(BookingFeatures.SlotLock, AppAction.Create, PermissionGroup.BookingOperations, "Acquire slot lock"),
        new(BookingFeatures.SlotLock, AppAction.Delete, PermissionGroup.BookingOperations, "Release slot lock"),

        // ── BookingAdmin (2) ────────────────────────────────────────────────
        new(BookingFeatures.BookingAdmin, AppAction.ReadAny, PermissionGroup.BookingOperations, "Admin: view all bookings"),
        new(BookingFeatures.BookingAdmin, AppAction.Update,  PermissionGroup.BookingOperations, "Admin: update any booking"),

        // ── BookingReports (1) ──────────────────────────────────────────────
        new(BookingFeatures.BookingReports, AppAction.Read, PermissionGroup.BookingOperations, "View booking reports"),

        // ── AdminBookingDashboard (2) ───────────────────────────────────────
        new(BookingFeatures.AdminBookingDashboard, AppAction.Read,   PermissionGroup.BookingOperations, "Admin: view booking dashboard (all bookings)"),
        new(BookingFeatures.AdminBookingDashboard, AppAction.Update, PermissionGroup.BookingOperations, "Admin: force refund / dashboard updates"),
    ];
}
