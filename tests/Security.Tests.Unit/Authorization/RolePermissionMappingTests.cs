using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Security.Contracts.Authorization;
using Security.Infrastructure.Seeding;
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Tests.Unit.Authorization;

/// <summary>
/// Unit tests for the P0/P1 authorization wave (2026-05-30) covering the
/// role-to-permission resolution rules in <see cref="RolePermissionMapping"/>.
///
/// Each test feeds a minimal in-memory <see cref="IPermissionCatalog"/> containing
/// only the descriptors needed to exercise the rule under test, so the assertions
/// are independent of the full (13-module) permission surface.
/// </summary>
public sealed class RolePermissionMappingTests
{
    private sealed record StubCatalog(IReadOnlyList<PermissionDescriptor> Permissions) : IPermissionCatalog
    {
        public string ModuleName => "Stub";
    }

    private static RolePermissionMapping Build(params PermissionDescriptor[] permissions) =>
        new(new[] { new StubCatalog(permissions) });

    private static PermissionDescriptor P(string feature, string action, string group) =>
        new(feature, action, group, $"{feature}.{action}");

    // ── P0: Outbox ops — Owner + SuperAdmin only; Admin must NOT receive them ──

    [Fact]
    public void Outbox_ops_permissions_are_granted_to_Owner_and_SuperAdmin_only()
    {
        var mapping = Build(
            P(SecurityFeatures.Outbox, AppAction.Read,   PermissionGroup.SystemAccess),
            P(SecurityFeatures.Outbox, AppAction.Replay, PermissionGroup.SystemAccess));

        var owner      = mapping.GetPermissionsForRole(AppRoles.Owner);
        var superAdmin = mapping.GetPermissionsForRole(AppRoles.SuperAdmin);
        var admin      = mapping.GetPermissionsForRole(AppRoles.Admin);

        owner.Should().Contain("Permission.Outbox.Read").And.Contain("Permission.Outbox.Replay");
        superAdmin.Should().Contain("Permission.Outbox.Read").And.Contain("Permission.Outbox.Replay");

        admin.Should().NotContain("Permission.Outbox.Read");
        admin.Should().NotContain("Permission.Outbox.Replay");
    }

    // ── P1: DeleteOwn / DeleteAny split ────────────────────────────────────────

    [Fact]
    public void DeleteOwn_is_granted_to_business_roles_but_DeleteAny_is_admin_only()
    {
        var mapping = Build(
            P("Tour", AppAction.DeleteOwn, PermissionGroup.ContentManagement),
            P("Tour", AppAction.DeleteAny, PermissionGroup.ContentManagement));

        var provider  = mapping.GetPermissionsForRole(AppRoles.Provider);
        var tourGuide = mapping.GetPermissionsForRole(AppRoles.TourGuide);
        var admin     = mapping.GetPermissionsForRole(AppRoles.Admin);

        // Business roles get the owner-scoped delete (via ProviderSelfPermissions).
        provider.Should().Contain("Permission.Tour.DeleteOwn");
        tourGuide.Should().Contain("Permission.Tour.DeleteOwn");

        // Business roles must NOT get the admin "any" delete.
        provider.Should().NotContain("Permission.Tour.DeleteAny");
        tourGuide.Should().NotContain("Permission.Tour.DeleteAny");

        // Admin+ get DeleteAny.
        admin.Should().Contain("Permission.Tour.DeleteAny");
    }

    [Fact]
    public void Plain_Delete_sweep_no_longer_grants_renamed_delete_permissions()
    {
        // A generic ContentManagement Delete should still be swept to business roles,
        // but the renamed Tour.DeleteAny (action == DeleteAny) must not be.
        var mapping = Build(
            P("Package", AppAction.Delete,    PermissionGroup.ContentManagement),
            P("Tour",    AppAction.DeleteAny, PermissionGroup.ContentManagement));

        var provider = mapping.GetPermissionsForRole(AppRoles.Provider);

        provider.Should().Contain("Permission.Package.Delete");
        provider.Should().NotContain("Permission.Tour.DeleteAny");
    }

    // ── P1: AgencyRoster re-scope — must NOT leak via ContentManagement sweep ──

    [Fact]
    public void AgencyRoster_create_delete_do_not_leak_to_Creator()
    {
        // Creator gets ContentManagement {Read,Create,Delete}. AgencyRoster.* shares
        // that group + actions but must be excluded from the sweep.
        var mapping = Build(
            P("AgencyRoster", AppAction.Create, PermissionGroup.ContentManagement),
            P("AgencyRoster", AppAction.Delete, PermissionGroup.ContentManagement));

        var creator = mapping.GetPermissionsForRole(AppRoles.Creator);

        creator.Should().NotContain("Permission.AgencyRoster.Create");
        creator.Should().NotContain("Permission.AgencyRoster.Delete");
    }

    [Fact]
    public void AgencyRoster_is_granted_to_Provider_and_TourGuide_for_self_service()
    {
        // Provider/TourGuide (agency-owning) receive AgencyRoster via ProviderSelfPermissions;
        // execution is still gated by the agency-ownership guard in the handlers.
        var mapping = Build(
            P("AgencyRoster", AppAction.Create, PermissionGroup.ContentManagement),
            P("AgencyRoster", AppAction.Delete, PermissionGroup.ContentManagement));

        var provider  = mapping.GetPermissionsForRole(AppRoles.Provider);
        var tourGuide = mapping.GetPermissionsForRole(AppRoles.TourGuide);

        provider.Should().Contain("Permission.AgencyRoster.Create").And.Contain("Permission.AgencyRoster.Delete");
        tourGuide.Should().Contain("Permission.AgencyRoster.Create").And.Contain("Permission.AgencyRoster.Delete");
    }

    // ── P1: Booking provider-side lifecycle permissions ───────────────────────

    [Fact]
    public void Provider_and_TourGuide_receive_booking_lifecycle_permissions()
    {
        var mapping = Build(
            P("TourBooking", AppAction.Confirm,  PermissionGroup.BookingOperations),
            P("TourBooking", AppAction.Complete, PermissionGroup.BookingOperations),
            P("TourBooking", AppAction.Reject,   PermissionGroup.BookingOperations),
            P("JoinRequest", AppAction.Approve,  PermissionGroup.BookingOperations),
            P("JoinRequest", AppAction.Reject,   PermissionGroup.BookingOperations));

        foreach (var role in new[] { AppRoles.Provider, AppRoles.TourGuide })
        {
            var perms = mapping.GetPermissionsForRole(role);
            perms.Should().Contain("Permission.TourBooking.Confirm");
            perms.Should().Contain("Permission.TourBooking.Complete");
            perms.Should().Contain("Permission.TourBooking.Reject");
            perms.Should().Contain("Permission.JoinRequest.Approve");
            perms.Should().Contain("Permission.JoinRequest.Reject");
        }
    }

    [Fact]
    public void User_does_not_receive_provider_booking_lifecycle_permissions()
    {
        var mapping = Build(
            P("TourBooking", AppAction.Confirm, PermissionGroup.BookingOperations),
            P("JoinRequest", AppAction.Approve, PermissionGroup.BookingOperations));

        var user = mapping.GetPermissionsForRole(AppRoles.User);

        user.Should().NotContain("Permission.TourBooking.Confirm");
        user.Should().NotContain("Permission.JoinRequest.Approve");
    }

    // ── P1: Provider provisioning — ProviderSelf + Consumer, no admin "Any" ───

    [Fact]
    public void Provider_receives_provider_self_and_consumer_permissions()
    {
        // ProviderDashboard.Read is in ProviderSelfPermissions; TourBooking.Create
        // (Cancel etc.) is in ConsumerPermissions.
        var mapping = Build(
            P("ProviderDashboard", AppAction.Read,   PermissionGroup.SystemAccess),
            P("TourBooking",       AppAction.Create, PermissionGroup.BookingOperations));

        var provider = mapping.GetPermissionsForRole(AppRoles.Provider);

        provider.Should().Contain("Permission.ProviderDashboard.Read");   // ProviderSelf
        provider.Should().Contain("Permission.TourBooking.Create");       // Consumer
    }

    [Fact]
    public void Provider_does_not_receive_admin_Any_permissions()
    {
        // Admin "Any"/moderation permissions live in non-ContentManagement groups
        // (or *.DeleteAny) and must never reach the Provider role.
        var mapping = Build(
            P("Tour",                  AppAction.DeleteAny, PermissionGroup.ContentManagement),
            P(SecurityFeatures.User,   AppAction.UpdateAny, PermissionGroup.SystemAccess),
            P("AdminProviderQueue",    AppAction.Approve,   PermissionGroup.ModerationTools),
            P("Payout",                AppAction.Trigger,   PermissionGroup.FinanceOperations));

        var provider = mapping.GetPermissionsForRole(AppRoles.Provider);

        provider.Should().NotContain("Permission.Tour.DeleteAny");
        provider.Should().NotContain("Permission.User.UpdateAny");
        provider.Should().NotContain("Permission.AdminProviderQueue.Approve");
        provider.Should().NotContain("Permission.Payout.Trigger");
    }

    // ── CCD-2/CCD-3 follow-up: creator self-service permissions ────────────────
    // Creator.{Update,Delete,RedeemInvitation} are group SystemAccess, so they are
    // NOT swept by any CRUD rule — they reach a role only via ConsumerPermissions.

    private static readonly string[] CreatorSelfServicePermissions =
    [
        "Permission.Creator.Update",
        "Permission.Creator.Delete",
        "Permission.Creator.RedeemInvitation",
    ];

    private static RolePermissionMapping BuildCreatorSelfServiceCatalog() =>
        Build(
            P(ContentBlogsCreatorFeature, AppAction.Update,           PermissionGroup.SystemAccess),
            P(ContentBlogsCreatorFeature, AppAction.Delete,           PermissionGroup.SystemAccess),
            P(ContentBlogsCreatorFeature, AppAction.RedeemInvitation, PermissionGroup.SystemAccess));

    private const string ContentBlogsCreatorFeature = "Creator";

    [Fact]
    public void User_receives_creator_self_service_permissions()
    {
        var user = BuildCreatorSelfServiceCatalog().GetPermissionsForRole(AppRoles.User);

        user.Should().Contain(CreatorSelfServicePermissions,
            "ordinary signed-in users must be able to edit/deactivate their own creator "
            + "profile and redeem invitations (CCD-2/CCD-3)");
    }

    [Fact]
    public void Creator_Provider_TourGuide_receive_same_consumer_propagation()
    {
        var mapping = BuildCreatorSelfServiceCatalog();

        foreach (var role in new[] { AppRoles.Creator, AppRoles.Provider, AppRoles.TourGuide })
        {
            mapping.GetPermissionsForRole(role)
                .Should().Contain(CreatorSelfServicePermissions,
                    $"role {role} also includes ConsumerPermissions");
        }
    }

    [Fact]
    public void Admin_Owner_SuperAdmin_already_cover_creator_self_service_permissions()
    {
        var mapping = BuildCreatorSelfServiceCatalog();

        foreach (var role in new[] { AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.Owner })
        {
            mapping.GetPermissionsForRole(role)
                .Should().Contain(CreatorSelfServicePermissions,
                    $"role {role} receives these via its full permission sweep");
        }
    }

    [Fact]
    public void Guest_does_not_receive_creator_self_service_permissions()
    {
        var guest = BuildCreatorSelfServiceCatalog().GetPermissionsForRole(AppRoles.Guest);

        guest.Should().NotContain("Permission.Creator.Update");
        guest.Should().NotContain("Permission.Creator.Delete");
        guest.Should().NotContain("Permission.Creator.RedeemInvitation");
    }
}
