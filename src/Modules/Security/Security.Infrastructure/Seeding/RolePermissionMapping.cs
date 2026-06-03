using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Infrastructure.Seeding;

/// <summary>
/// Maps roles to permissions by querying all registered <see cref="IPermissionCatalog"/> instances.
/// Replaces the static switch in <c>AppPermissions.GetPermissionsForRole</c>.
/// Adding a new module's catalog to DI automatically adds its permissions here.
/// </summary>
public sealed class RolePermissionMapping
{
    private readonly IReadOnlyList<PermissionDescriptor> _all;

    // Oracle 2026-05-29: _all was never initialized — every call to GetPermissionsForRole
    // threw NullReferenceException, which Program.cs SecurityDataSeeder catch swallowed
    // silently, leaving role claims stuck on the SecurityDbInitializer's lowercase
    // bootstrap set. Initializing from registered IPermissionCatalog instances is the
    // single source of truth per agent-context.md Rule 4.
    public RolePermissionMapping(IEnumerable<IPermissionCatalog> catalogs)
    {
        _all = catalogs
            .SelectMany(catalog => catalog.Permissions)
            .GroupBy(permission => permission.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    // Consumer self-service permissions that every signed-in user (User, TourGuide)
    // needs to operate the public app: read/update own profile, book tours, leave
    // reviews, manage wishlist and notifications. Added 2026-05-29 to fix
    // Findings F4 + F9 — without these the User role gets 403 on /accounts/profile,
    // /booking/my-bookings, and cannot create a booking, blocking the whole
    // consumer workflow described in Booking-Workflow.md §4.
    private static readonly HashSet<string> ConsumerPermissions =
        new(StringComparer.Ordinal)
        {
            "Permission.Profile.Read",
            "Permission.Profile.Update",
            "Permission.Account.Read",
            "Permission.Account.Update",
            "Permission.TourBooking.Create",
            "Permission.TourBooking.ReadOwn",
            "Permission.TourBooking.Cancel",
            "Permission.Review.Create",
            "Permission.ReviewReply.Create",
            "Permission.Favorite.Create",
            "Permission.Favorite.Read",
            "Permission.Favorite.Delete",
            "Permission.Notification.Read",
            "Permission.Notification.Update",
            "Permission.NotificationPreference.Read",
            "Permission.NotificationPreference.Update",
            "Permission.DeviceToken.Create",
            "Permission.DeviceToken.Delete",
            "Permission.Preference.Read",
            "Permission.Preference.Update",
            "Permission.SupportTicket.Create",
            "Permission.SupportTicket.Read",
            // F94 2026-05-30: User can close own support ticket.
            "Permission.SupportTicket.Close",
            "Permission.Report.Create",
            // F79 2026-05-30: User views own provider application status after register.
            "Permission.ProviderApplication.Read",
            // F87 2026-05-30: User views own auth sessions.
            "Permission.Session.Read",
            // F117 2026-05-30: User logs out (POST /auth/logout + /logout-all + DELETE /sessions/{id}).
            "Permission.Session.Delete",
            // F96 2026-05-30: User reads + posts blog comments.
            "Permission.BlogComment.Create",
            "Permission.BlogComment.Read",
            // F101 2026-05-30: User submits tour proposals (Create + Read + Submit Draft).
            "Permission.TourProposal.Create",
            "Permission.TourProposal.Read",
            "Permission.TourProposal.Submit",
            // F17/F18 2026-05-29: User role needs Recommendation.Read for personalized
            // recommendations and Refund.{Create,Read} for self-opened disputes
            // (workflow §5 — anyone can dispute their own booking; group at /disputes).
            "Permission.Recommendation.Read",
            "Permission.Refund.Read",
            "Permission.Refund.Create",

            // F30 2026-05-30: User couldn't read own reviews via /social/reviews/my-reviews → 403
            "Permission.Review.Read",

            // F33 2026-05-30: User couldn't list own device tokens via /devices/tokens → 403
            "Permission.DeviceToken.Read",

            // F44 2026-05-30: User couldn't apply to become provider via /provider/apply → 403
            // Real perms: /apply requires AccountsFeatures.ProviderApplication+AppAction.Submit
            // and /register requires AccountsFeatures.ProviderApplication+AppAction.Register
            "Permission.ProviderApplication.Submit",
            "Permission.ProviderApplication.Register",
            "Permission.ProviderApplication.Create",
            "Permission.ProviderApplication.Update",

            // F53 2026-05-30: User couldn't soft-delete own profile via DELETE /accounts/profile → 403 (GDPR)
            "Permission.Profile.Delete",
            "Permission.Profile.SoftDelete",

            // F69 2026-05-30: User couldn't apply to become creator via /blogs/creators/applications → 403
            "Permission.Creator.Submit",
            "Permission.Creator.Read",
        };

    // Provider self-service permissions that an approved provider (TourGuide,
    // Business, Agency — all carry the TourGuide identity role today) needs to
    // run their own listings: apply, upload docs, view dashboard, manage payment
    // methods/bank accounts, manage own tours/slots/offerings. Added 2026-05-29
    // to fix Finding F3 — /provider/{status,dashboard} returned 403 for an
    // approved guide; no self-service surface worked.
    private static readonly HashSet<string> ProviderSelfPermissions =
        new(StringComparer.Ordinal)
        {
            "Permission.ProviderApplication.Create",
            "Permission.ProviderApplication.Read",
            "Permission.ProviderApplication.Update",
            "Permission.ProviderDocument.Create",
            "Permission.ProviderDocument.Read",
            "Permission.ProviderDocument.Delete",
            "Permission.ProviderDashboard.Read",
            "Permission.ProviderPaymentMethod.Create",
            "Permission.ProviderPaymentMethod.Read",
            "Permission.ProviderPaymentMethod.Update",
            "Permission.ProviderPaymentMethod.Delete",
            "Permission.ProviderBankAccount.Create",
            "Permission.ProviderBankAccount.Read",
            "Permission.ProviderBankAccount.Update",
            "Permission.ProviderBankAccount.Delete",
            "Permission.GuideOffering.Create",
            "Permission.GuideOffering.Read",
            "Permission.GuideOffering.Update",
            "Permission.GuideOffering.Delete",
            "Permission.GuideAgency.Read",
            "Permission.GuideDashboard.Read",
            "Permission.GuideApplication.Create",
            "Permission.GuideApplication.Read",
            "Permission.Tour.Create",
            "Permission.Tour.Update",
            // P1 DeleteOwn/DeleteAny split (2026-05-30): owner-scoped delete.
            // Tour.Delete was renamed to Tour.DeleteAny (admin-only); providers
            // get the owner-scoped variant. Handler enforces ownership.
            "Permission.Tour.DeleteOwn",
            "Permission.Tour.ReadOwn",
            "Permission.Tour.Submit",
            "Permission.Tour.Archive",
            "Permission.AvailabilitySlot.Create",
            "Permission.AvailabilitySlot.Read",
            "Permission.AvailabilitySlot.Update",
            "Permission.AvailabilitySlot.Delete",
            "Permission.TourPricingTier.Create",
            "Permission.TourPricingTier.Read",
            "Permission.TourPricingTier.Update",
            "Permission.TourPricingTier.Delete",
            "Permission.TourSchedule.Create",
            "Permission.TourSchedule.Read",
            "Permission.TourSchedule.Update",
            "Permission.TourSchedule.Delete",
            "Permission.Payout.Read",
            "Permission.Invoice.Read",

            // P1 Booking provider-side lifecycle (2026-05-30): provider/guide may
            // confirm/complete/reject OWN bookings and approve/reject join requests
            // for tours they own. Handlers enforce provider-ownership; Admin+ retain
            // override via AdminBookingDashboard.{Read,Update}.
            "Permission.TourBooking.Confirm",
            "Permission.TourBooking.Complete",
            "Permission.TourBooking.Reject",
            "Permission.JoinRequest.Approve",
            "Permission.JoinRequest.Reject",

            // P1 TourGuideProfile owner-scoped self-deactivation (DeleteOwn split).
            "Permission.TourGuideProfile.DeleteOwn",

            // P1 AgencyRoster re-scope (2026-05-30): the agency-owning provider may
            // attempt invite/remove/approve/reject of guides. The "may execute" check
            // is enforced by the agency-ownership guard in the command handlers — the
            // permission alone does NOT grant cross-agency access. Moved here OUT of
            // the generic ContentManagement CRUD sweep so it no longer leaks to every
            // content role globally.
            "Permission.AgencyRoster.Read",
            "Permission.AgencyRoster.Create",
            "Permission.AgencyRoster.Delete",
            "Permission.AgencyRoster.Approve",
            "Permission.AgencyRoster.Reject",
        };

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) =>
        roleName switch
        {
            AppRoles.Owner =>
                _all.Select(p => p.Name).ToList(),

            AppRoles.SuperAdmin =>
                _all.Where(p => !IsOwnerOnly(p))
                    .Select(p => p.Name).ToList(),

            AppRoles.Admin =>
                _all.Where(p => !IsOwnerOnly(p) && !IsSuperAdminOnly(p))
                    .Select(p => p.Name).ToList(),

            // P1 (2026-05-30): Provider is now a real self-service role — it gets
            // ProviderSelfPermissions AND ConsumerPermissions (decision #4), not just
            // ContentManagement CRUD. AgencyRoster.* is excluded from the CRUD sweep
            // (see IsAgencyRoster) so it no longer leaks; instead it is granted only
            // through ProviderSelfPermissions and gated by the agency-ownership guard.
            AppRoles.Provider =>
                _all.Where(p => (p.Group == PermissionGroup.ContentManagement
                                 && !IsAgencyRoster(p)
                                 && p.Action is AppAction.Read or AppAction.Create
                                     or AppAction.Update or AppAction.Delete)
                             || (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible
                             || ConsumerPermissions.Contains(p.Name)
                             || ProviderSelfPermissions.Contains(p.Name))
                    .Select(p => p.Name).ToList(),

            AppRoles.Creator =>
                _all.Where(p => (p.Group == PermissionGroup.ContentManagement
                                 && !IsAgencyRoster(p)
                                 && p.Action is AppAction.Read or AppAction.Create or AppAction.Delete)
                             || (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible
                             // Owner-scoped blog delete (DeleteOwn split); Blog.Delete
                             // was renamed to Blog.DeleteAny (admin-only).
                             || (p.Feature == "Blog" && p.Action == AppAction.DeleteOwn)
                             || ConsumerPermissions.Contains(p.Name))
                    .Select(p => p.Name).ToList(),

            AppRoles.User =>
                _all.Where(p => (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible
                             || ConsumerPermissions.Contains(p.Name))
                    .Select(p => p.Name).ToList(),

            AppRoles.TourGuide =>
                _all.Where(p => (p.Group == PermissionGroup.ContentManagement
                                 && !IsAgencyRoster(p)
                                 && (p.Action == AppAction.Read || p.Action == AppAction.Create || p.Action == AppAction.Delete))
                             || (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible
                             // Owner-scoped blog delete (DeleteOwn split).
                             || (p.Feature == "Blog" && p.Action == AppAction.DeleteOwn)
                             || ConsumerPermissions.Contains(p.Name)
                             || ProviderSelfPermissions.Contains(p.Name))
                    .Select(p => p.Name).ToList(),

            AppRoles.Guest =>
                _all.Where(p => p.IsGuestAccessible)
                    .Select(p => p.Name).ToList(),

            _ => []
        };

    // Outbox ops are intentionally NOT owner-only — SuperAdmin keeps them (see IsOpsOnly).
    private static bool IsOwnerOnly(PermissionDescriptor p) =>
        p.Feature == SecurityFeatures.System && p.Action == AppAction.Update;

    // P0 (2026-05-30): Outbox ops are restricted from Admin (Owner + SuperAdmin only).
    // The Admin branch excludes IsSuperAdminOnly, so flagging Outbox here removes it from
    // Admin while the SuperAdmin branch (excludes only IsOwnerOnly) keeps it.
    private static bool IsSuperAdminOnly(PermissionDescriptor p) =>
        (p.Feature == SecurityFeatures.User && p.Action == AppAction.DeleteAny)
        || IsOpsOnly(p);

    // P0: Operational outbox permissions — Owner + SuperAdmin only.
    private static bool IsOpsOnly(PermissionDescriptor p) =>
        p.Feature == SecurityFeatures.Outbox;

    // P1: AgencyRoster permissions are scoped to agency owners + Admin+, NOT the
    // generic ContentManagement CRUD sweep. Excluded from the business-role sweeps.
    private static bool IsAgencyRoster(PermissionDescriptor p) =>
        p.Feature == "AgencyRoster";
}
