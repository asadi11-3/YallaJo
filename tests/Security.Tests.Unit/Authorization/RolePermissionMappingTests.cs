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

    // ── FE-1A: BookingDispute — Create is consumer-wide, Resolve is admin-only ──

    private static RolePermissionMapping BuildBookingDisputeCatalog() =>
        Build(
            P("BookingDispute", AppAction.Create,  PermissionGroup.BookingOperations),
            P("BookingDispute", AppAction.Resolve, PermissionGroup.BookingOperations),
            P("BookingDispute", AppAction.Read,    PermissionGroup.BookingOperations));

    [Fact]
    public void BookingDispute_Create_is_granted_to_consumer_roles()
    {
        var mapping = BuildBookingDisputeCatalog();

        foreach (var role in new[]
                 {
                     AppRoles.User, AppRoles.Provider, AppRoles.TourGuide, AppRoles.Creator,
                 })
        {
            mapping.GetPermissionsForRole(role)
                .Should().Contain("Permission.BookingDispute.Create",
                    $"role {role} must be able to open a dispute on its own completed booking (FE-1A)");
        }
    }

    [Fact]
    public void BookingDispute_Resolve_is_not_granted_to_consumer_roles()
    {
        var mapping = BuildBookingDisputeCatalog();

        foreach (var role in new[]
                 {
                     AppRoles.User, AppRoles.Provider, AppRoles.TourGuide, AppRoles.Creator,
                 })
        {
            mapping.GetPermissionsForRole(role)
                .Should().NotContain("Permission.BookingDispute.Resolve",
                    $"role {role} must NOT be able to resolve disputes (admin-only)");
        }
    }

    [Fact]
    public void BookingDispute_Resolve_is_granted_to_Admin_SuperAdmin_Owner()
    {
        var mapping = BuildBookingDisputeCatalog();

        foreach (var role in new[] { AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.Owner })
        {
            var perms = mapping.GetPermissionsForRole(role);
            perms.Should().Contain("Permission.BookingDispute.Resolve",
                $"role {role} resolves disputes via its full permission sweep");
            perms.Should().Contain("Permission.BookingDispute.Create",
                $"role {role} also receives Create via its full sweep");
        }
    }

    [Fact]
    public void BookingDispute_permissions_not_granted_to_Guest()
    {
        var guest = BuildBookingDisputeCatalog().GetPermissionsForRole(AppRoles.Guest);

        guest.Should().NotContain("Permission.BookingDispute.Create");
        guest.Should().NotContain("Permission.BookingDispute.Resolve");
    }

    // ── FE-1B: Notification.Delete — consumer-wide, never Guest ────────────────

    private static RolePermissionMapping BuildNotificationCatalog() =>
        Build(
            P("Notification", AppAction.Read,   PermissionGroup.SupportOperations),
            P("Notification", AppAction.Update, PermissionGroup.SupportOperations),
            P("Notification", AppAction.Delete, PermissionGroup.SupportOperations));

    [Fact]
    public void Notification_Delete_is_granted_to_consumer_roles()
    {
        var mapping = BuildNotificationCatalog();

        foreach (var role in new[]
                 {
                     AppRoles.User, AppRoles.Provider, AppRoles.TourGuide, AppRoles.Creator,
                 })
        {
            mapping.GetPermissionsForRole(role)
                .Should().Contain("Permission.Notification.Delete",
                    $"role {role} must be able to delete its own notifications (FE-1B)");
        }
    }

    [Fact]
    public void Notification_Delete_is_granted_to_Admin_SuperAdmin_Owner()
    {
        var mapping = BuildNotificationCatalog();

        foreach (var role in new[] { AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.Owner })
        {
            mapping.GetPermissionsForRole(role)
                .Should().Contain("Permission.Notification.Delete",
                    $"role {role} receives Notification.Delete via its full permission sweep");
        }
    }

    [Fact]
    public void Notification_Delete_is_not_granted_to_Guest()
    {
        var guest = BuildNotificationCatalog().GetPermissionsForRole(AppRoles.Guest);

        guest.Should().NotContain("Permission.Notification.Delete");
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

    // ── CCD-4: creator article authoring (Blog.{ReadOwn,Update,Submit}) ─────────
    // Mirrors the real catalog grouping: Blog.{Read,Create,Update,DeleteOwn} are
    // ContentManagement; Blog.{ReadOwn,Submit} are SystemAccess. The Creator role
    // must end up with all five article-authoring permissions.

    private static RolePermissionMapping BuildBlogArticleCatalog() =>
        Build(
            P("Blog", AppAction.Read,      PermissionGroup.ContentManagement),
            P("Blog", AppAction.Create,    PermissionGroup.ContentManagement),
            P("Blog", AppAction.Update,    PermissionGroup.ContentManagement),
            P("Blog", AppAction.DeleteOwn, PermissionGroup.ContentManagement),
            P("Blog", AppAction.ReadOwn,   PermissionGroup.SystemAccess),
            P("Blog", AppAction.Submit,    PermissionGroup.SystemAccess));

    private static readonly string[] CreatorArticlePermissions =
    [
        "Permission.Blog.Read",       // admin-get edit prefetch
        "Permission.Blog.Create",     // create draft
        "Permission.Blog.Update",     // update
        "Permission.Blog.DeleteOwn",  // delete + restore
        "Permission.Blog.ReadOwn",    // my-blogs
        "Permission.Blog.Submit",     // submit for review
    ];

    [Fact]
    public void Creator_receives_all_article_authoring_permissions()
    {
        var creator = BuildBlogArticleCatalog().GetPermissionsForRole(AppRoles.Creator);

        creator.Should().Contain(CreatorArticlePermissions,
            "an approved creator must be able to list, create, edit, submit, delete and "
            + "restore their own articles (CCD-4)");
    }

    [Fact]
    public void Creator_article_grant_includes_the_three_previously_missing_permissions()
    {
        // Regression guard for the CCD-4 prerequisite: Update (ContentManagement, but
        // excluded from the Creator sweep) + ReadOwn/Submit (SystemAccess, never swept).
        var creator = BuildBlogArticleCatalog().GetPermissionsForRole(AppRoles.Creator);

        creator.Should().Contain("Permission.Blog.Update");
        creator.Should().Contain("Permission.Blog.ReadOwn");
        creator.Should().Contain("Permission.Blog.Submit");
    }

    [Fact]
    public void User_does_not_receive_creator_article_authoring_permissions()
    {
        // The base User role must NOT gain article authoring (it is granted only to the
        // Creator role, not via ConsumerPermissions).
        var user = BuildBlogArticleCatalog().GetPermissionsForRole(AppRoles.User);

        user.Should().NotContain("Permission.Blog.ReadOwn");
        user.Should().NotContain("Permission.Blog.Update");
        user.Should().NotContain("Permission.Blog.Submit");
        user.Should().NotContain("Permission.Blog.Create");
    }

    [Fact]
    public void Admin_still_covers_article_authoring_permissions()
    {
        var admin = BuildBlogArticleCatalog().GetPermissionsForRole(AppRoles.Admin);

        admin.Should().Contain(CreatorArticlePermissions,
            "Admin retains article permissions via its full sweep");
    }

    // ── CCD-5: creator article image management (Attachment/EntityImage) ────────
    // All Attachment/EntityImage actions are ContentManagement. The Creator sweep
    // grants Read/Create/Delete; the prerequisite follow-up adds the Update action
    // for Attachment (reorder) and EntityImage (set primary), Creator role only.

    private static RolePermissionMapping BuildAttachmentCatalog() =>
        Build(
            P("Attachment", AppAction.Read,   PermissionGroup.ContentManagement),
            P("Attachment", AppAction.Create, PermissionGroup.ContentManagement),
            P("Attachment", AppAction.Update, PermissionGroup.ContentManagement),
            P("Attachment", AppAction.Delete, PermissionGroup.ContentManagement),
            P("EntityImage", AppAction.Read,   PermissionGroup.ContentManagement),
            P("EntityImage", AppAction.Create, PermissionGroup.ContentManagement),
            P("EntityImage", AppAction.Update, PermissionGroup.ContentManagement),
            P("EntityImage", AppAction.Delete, PermissionGroup.ContentManagement));

    private static readonly string[] CreatorImagePermissions =
    [
        "Permission.Attachment.Read",    // list images
        "Permission.Attachment.Create",  // upload
        "Permission.Attachment.Delete",  // delete
        "Permission.Attachment.Update",  // reorder
        "Permission.EntityImage.Update", // set primary
    ];

    [Fact]
    public void Creator_receives_all_article_image_permissions()
    {
        var creator = BuildAttachmentCatalog().GetPermissionsForRole(AppRoles.Creator);

        creator.Should().Contain(CreatorImagePermissions,
            "an approved creator must be able to list, upload, delete, reorder and set "
            + "the primary image for their own articles (CCD-5)");
    }

    [Fact]
    public void Creator_image_grant_includes_the_two_previously_missing_update_permissions()
    {
        // Regression guard for the CCD-5 prerequisite: the two Update actions excluded
        // from the Creator Read/Create/Delete sweep.
        var creator = BuildAttachmentCatalog().GetPermissionsForRole(AppRoles.Creator);

        creator.Should().Contain("Permission.Attachment.Update");
        creator.Should().Contain("Permission.EntityImage.Update");
    }

    [Fact]
    public void User_does_not_receive_article_image_update_permissions()
    {
        // The base User role must NOT gain image management (granted only to Creator).
        var user = BuildAttachmentCatalog().GetPermissionsForRole(AppRoles.User);

        user.Should().NotContain("Permission.Attachment.Update");
        user.Should().NotContain("Permission.EntityImage.Update");
        user.Should().NotContain("Permission.Attachment.Create");
    }

    [Fact]
    public void Admin_still_covers_article_image_permissions()
    {
        var admin = BuildAttachmentCatalog().GetPermissionsForRole(AppRoles.Admin);

        admin.Should().Contain(CreatorImagePermissions,
            "Admin retains image permissions via its full sweep");
    }

    // ── FE-2D: accessibility reviews — authored by regular travelers ────────────
    // All four AccessibilityReview actions are ContentManagement, but accessibility
    // reviews are written by ordinary users, so the consumer roles need the full CRUD
    // set (granted via ConsumerPermissions, mirroring the normal Review.* grants).

    private static RolePermissionMapping BuildAccessibilityReviewCatalog() =>
        Build(
            P("AccessibilityReview", AppAction.Read,   PermissionGroup.ContentManagement),
            P("AccessibilityReview", AppAction.Create, PermissionGroup.ContentManagement),
            P("AccessibilityReview", AppAction.Update, PermissionGroup.ContentManagement),
            P("AccessibilityReview", AppAction.Delete, PermissionGroup.ContentManagement));

    private static readonly string[] AccessibilityReviewPermissions =
    [
        "Permission.AccessibilityReview.Read",
        "Permission.AccessibilityReview.Create",
        "Permission.AccessibilityReview.Update",
        "Permission.AccessibilityReview.Delete",
    ];

    [Fact]
    public void User_receives_all_accessibility_review_permissions()
    {
        // The base User role (regular traveler) is the primary author of accessibility
        // reviews, so it must receive the full CRUD set via ConsumerPermissions — note
        // Update in particular is excluded from every business-role ContentManagement sweep.
        var user = BuildAccessibilityReviewCatalog().GetPermissionsForRole(AppRoles.User);

        user.Should().Contain(AccessibilityReviewPermissions,
            "a regular traveler must be able to read, create, edit (within 48h) and delete "
            + "their own accessibility reviews (FE-2D); ownership and the edit window are "
            + "enforced server-side");
    }

    [Theory]
    [InlineData("Creator")]
    [InlineData("Provider")]
    [InlineData("TourGuide")]
    public void Consumer_roles_receive_all_accessibility_review_permissions(string role)
    {
        // The Update action is excluded from the business-role Read/Create/Delete sweeps,
        // so without the ConsumerPermissions grant these roles could not edit. Verify the
        // grant reaches every consumer role.
        var perms = BuildAccessibilityReviewCatalog().GetPermissionsForRole(role);

        perms.Should().Contain(AccessibilityReviewPermissions);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    [InlineData("Owner")]
    public void Admin_tier_still_covers_accessibility_review_permissions(string role)
    {
        var perms = BuildAccessibilityReviewCatalog().GetPermissionsForRole(role);

        perms.Should().Contain(AccessibilityReviewPermissions,
            "the admin tier retains accessibility-review permissions via its full sweep");
    }
}
