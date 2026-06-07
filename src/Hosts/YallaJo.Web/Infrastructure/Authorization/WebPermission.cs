namespace YallaJo.Web.Infrastructure.Authorization;

/// <summary>
/// All permission name constants used in YallaJo.Web, organized by feature.
///
/// Values match the format produced by AppPermission.NameFor() on the backend:
///   "Permission.{Feature}.{Action}"
///
/// RULE: No permission string may be constructed inline anywhere else in the project.
///       All permission checks go through ICurrentUser.HasPermission(WebPermission.X.Y).
/// </summary>
public static class WebPermission
{
    // ── Role ──────────────────────────────────────────────────────────────────
    public static class Role
    {
        public const string Read   = "Permission.Role.Read";
        public const string Create = "Permission.Role.Create";
        public const string Update = "Permission.Role.Update";
        public const string Delete = "Permission.Role.Delete";
    }

    // ── RoleClaim ─────────────────────────────────────────────────────────────
    public static class RoleClaim
    {
        public const string Read   = "Permission.RoleClaim.Read";
        public const string Create = "Permission.RoleClaim.Create";
        public const string Delete = "Permission.RoleClaim.Delete";
    }

    // ── User ──────────────────────────────────────────────────────────────────
    public static class User
    {
        public const string Read       = "Permission.User.Read";
        public const string Create     = "Permission.User.Create";
        public const string UpdateAny  = "Permission.User.UpdateAny";
        public const string DeleteAny  = "Permission.User.DeleteAny";
        public const string UpdateSelf = "Permission.User.UpdateSelf";
        public const string SoftDelete = "Permission.User.SoftDelete";
    }

    // ── UserRole ──────────────────────────────────────────────────────────────
    public static class UserRole
    {
        public const string Create = "Permission.UserRole.Create";
        public const string Delete = "Permission.UserRole.Delete";
    }

    // ── System ────────────────────────────────────────────────────────────────
    public static class System
    {
        public const string Read   = "Permission.System.Read";
        public const string Update = "Permission.System.Update";
    }

    // ── Outbox (ops: outbox dead-letter management) ─────────────────────────────
    // Mirror the SharedKernel ops permissions (Permission.Outbox.{Action}).
    public static class Outbox
    {
        public const string Read   = "Permission.Outbox.Read";
        public const string Replay = "Permission.Outbox.Replay";
    }

    // ── Category ─────────────────────────────────────────────────────────────
    public static class Category
    {
        public const string Read   = "Permission.Category.Read";
        public const string Create = "Permission.Category.Create";
        public const string Update = "Permission.Category.Update";
        public const string Delete = "Permission.Category.Delete";
    }

    // ── Specialization ────────────────────────────────────────────────────────
    public static class Specialization
    {
        public const string Read   = "Permission.Specialization.Read";
        public const string Create = "Permission.Specialization.Create";
        public const string Update = "Permission.Specialization.Update";
        public const string Delete = "Permission.Specialization.Delete";
    }

    // ── Language ──────────────────────────────────────────────────────────────
    public static class Language
    {
        public const string Read   = "Permission.Language.Read";
        public const string Create = "Permission.Language.Create";
        public const string Update = "Permission.Language.Update";
        public const string Delete = "Permission.Language.Delete";
    }

    // ── Tag ───────────────────────────────────────────────────────────────────
    public static class Tag
    {
        public const string Read   = "Permission.Tag.Read";
        public const string Create = "Permission.Tag.Create";
        public const string Update = "Permission.Tag.Update";
        public const string Delete = "Permission.Tag.Delete";
    }

    public static class EntityTag
    {
        public const string Read   = "Permission.EntityTag.Read";
        public const string Create = "Permission.EntityTag.Create";
        public const string Delete = "Permission.EntityTag.Delete";
    }

    public static class EntityCategory
    {
        public const string Read   = "Permission.EntityCategory.Read";
        public const string Create = "Permission.EntityCategory.Create";
        public const string Delete = "Permission.EntityCategory.Delete";
    }

    // ── SEO (ContentSeo module) ───────────────────────────────────────────────
    public static class Redirect
    {
        public const string Read   = "Permission.Redirect.Read";
        public const string Create = "Permission.Redirect.Create";
        public const string Update = "Permission.Redirect.Update";
        public const string Delete = "Permission.Redirect.Delete";
    }

    public static class SeoMetadata
    {
        public const string Read   = "Permission.SeoMetadata.Read";
        public const string Create = "Permission.SeoMetadata.Create";
        public const string Update = "Permission.SeoMetadata.Update";
    }

    public static class Sitemap
    {
        public const string Read    = "Permission.Sitemap.Read";
        public const string Update  = "Permission.Sitemap.Update";
        public const string Delete  = "Permission.Sitemap.Delete";
        public const string Refresh = "Permission.Sitemap.Refresh";
    }

    public static class Weather
    {
        public const string Read    = "Permission.Weather.Read";
        public const string Refresh = "Permission.Weather.Refresh";
        public const string Update  = "Permission.Weather.Update";
        public const string Delete  = "Permission.Weather.Delete";
    }

    public static class FaqItem
    {
        public const string Read   = "Permission.FaqItem.Read";
        public const string Create = "Permission.FaqItem.Create";
        public const string Update = "Permission.FaqItem.Update";
        public const string Delete = "Permission.FaqItem.Delete";
    }

    // ── Place ─────────────────────────────────────────────────────────────────
    public static class Place
    {
        public const string Read   = "Permission.Place.Read";
        public const string Create = "Permission.Place.Create";
        public const string Update = "Permission.Place.Update";
        public const string Delete = "Permission.Place.Delete";
        // Owner-scoped delete — mirrors the backend DELETE /api/v1/places/{id}
        // endpoint which is gated on Permission.Place.DeleteOwn (admins satisfy
        // it via the handler's admin-tier / Place.DeleteAny override).
        public const string DeleteOwn = "Permission.Place.DeleteOwn";
        public const string SoftDelete = "Permission.Place.SoftDelete";
    }

    // ── Attachment ────────────────────────────────────────────────────────────
    public static class Attachment
    {
        public const string Read   = "Permission.Attachment.Read";
        public const string Create = "Permission.Attachment.Create";
        public const string Update = "Permission.Attachment.Update";
        public const string Delete = "Permission.Attachment.Delete";
    }

    // ── EntityImage ───────────────────────────────────────────────────────────
    public static class EntityImage
    {
        public const string Update = "Permission.EntityImage.Update";
    }

    // ── TranslationCache ──────────────────────────────────────────────────────
    public static class TranslationCache
    {
        public const string Read   = "Permission.TranslationCache.Read";
        public const string Create = "Permission.TranslationCache.Create";
        public const string Update = "Permission.TranslationCache.Update";
    }

    // ── Booking ───────────────────────────────────────────────────────────────
    public static class Booking
    {
        public const string Read   = "Permission.Booking.Read";
        public const string Create = "Permission.Booking.Create";
        public const string Update = "Permission.Booking.Update";
        public const string Delete = "Permission.Booking.Delete";
    }

    // ── Payment (Finance — Permission.Payment.*) ──────────────────────────────────
    // Granted to the User/traveler role (PAY-0) so travelers can initiate + read
    // payments for their own bookings.
    public static class Payment
    {
        public const string Create = "Permission.Payment.Create";
        public const string Read   = "Permission.Payment.Read";
    }

    // ── Invoice (Finance — Permission.Invoice.*) ──────────────────────────────────
    // Granted to the User/traveler role so buyers can list + download invoices
    // for their own bookings.
    public static class Invoice
    {
        public const string Read     = "Permission.Invoice.Read";
        public const string Download = "Permission.Invoice.Download";
    }

    // ── ProviderPaymentMethod (Finance — Permission.ProviderPaymentMethod.*) ───────
    // Granted to the Provider role so providers manage their own payout methods.
    public static class ProviderPaymentMethod
    {
        public const string Read   = "Permission.ProviderPaymentMethod.Read";
        public const string Create = "Permission.ProviderPaymentMethod.Create";
        public const string Update = "Permission.ProviderPaymentMethod.Update";
        public const string Delete = "Permission.ProviderPaymentMethod.Delete";
    }

    // ── AdminBookingDashboard (Permission.AdminBookingDashboard.*) ─────────────────
    // Backs the admin cross-provider booking dashboard (AB-1). Granted to
    // Admin/SuperAdmin/Owner via the catalog-driven role sweep.
    public static class AdminBookingDashboard
    {
        public const string Read   = "Permission.AdminBookingDashboard.Read";
        public const string Update = "Permission.AdminBookingDashboard.Update";
    }

    // ── TourWaypoint ──────────────────────────────────────────────────────────
    public static class TourWaypoint
    {
        public const string Create = "Permission.TourWaypoint.Create";
        public const string Update = "Permission.TourWaypoint.Update";
        public const string Delete = "Permission.TourWaypoint.Delete";
    }

    // ── TourChildrenInfo ──────────────────────────────────────────────────────
    public static class TourChildrenInfo
    {
        public const string Update = "Permission.TourChildrenInfo.Update";
    }

    // ── Package (tour packages) ───────────────────────────────────────────────
    public static class Package
    {
        public const string Create = "Permission.Package.Create";
        public const string Update = "Permission.Package.Update";
        public const string Delete = "Permission.Package.Delete";
    }

    // ── TourGuide (assign guides to a tour) ───────────────────────────────────
    public static class TourGuide
    {
        public const string Update = "Permission.TourGuide.Update";
    }

    // ── TourGuideProfile (guide self profile + admin moderation) ──────────────
    public static class TourGuideProfile
    {
        public const string Read      = "Permission.TourGuideProfile.Read";
        public const string DeleteOwn = "Permission.TourGuideProfile.DeleteOwn";
        public const string Suspend   = "Permission.TourGuideProfile.Suspend";
        public const string Reinstate = "Permission.TourGuideProfile.Reinstate";
        public const string Update    = "Permission.TourGuideProfile.Update";
        public const string DeleteAny = "Permission.TourGuideProfile.DeleteAny";
    }

    // ── TourBooking (real backend booking permissions — Permission.TourBooking.*) ──
    // Granted to approved Provider/TourGuide roles (Confirm/Reject/Complete via
    // ProviderSelfPermissions; ReadOwn/Cancel via the consumer set). Used for the
    // provider booking-management screens (PB-1/PB-2).
    public static class TourBooking
    {
        public const string ReadOwn  = "Permission.TourBooking.ReadOwn";
        public const string Create   = "Permission.TourBooking.Create";
        public const string Cancel   = "Permission.TourBooking.Cancel";
        public const string Confirm  = "Permission.TourBooking.Confirm";
        public const string Complete = "Permission.TourBooking.Complete";
        public const string Reject   = "Permission.TourBooking.Reject";
    }

    // ── BookingDispute (FE-1A — owner opens / admin resolves a booking dispute) ──
    // Mirror the Booking backend permissions (Permission.BookingDispute.{Action}).
    // Create is granted to consumer roles (open own dispute); Resolve is admin-only.
    public static class BookingDispute
    {
        public const string Create  = "Permission.BookingDispute.Create";
        public const string Resolve = "Permission.BookingDispute.Resolve";
        public const string Read    = "Permission.BookingDispute.Read";
    }

    // ── JoinRequest (group-booking join queue) ────────────────────────────────
    public static class JoinRequest
    {
        public const string ReadOwn = "Permission.JoinRequest.ReadOwn";
        public const string Create  = "Permission.JoinRequest.Create";
        public const string Approve = "Permission.JoinRequest.Approve";
        public const string Reject  = "Permission.JoinRequest.Reject";
    }

    // ── Payout (provider earnings/payouts + admin batch moderation) ───────────
    public static class Payout
    {
        public const string Read    = "Permission.Payout.Read";
        public const string Trigger = "Permission.Payout.Trigger";
        public const string Approve = "Permission.Payout.Approve";
    }

    // ── Refund (provider disputes) ────────────────────────────────────────────
    public static class Refund
    {
        public const string Read   = "Permission.Refund.Read";
        public const string Create = "Permission.Refund.Create";
    }

    // ── DeviceToken (Messaging — push-notification device tokens, §3.10 Devices) ──
    // Mirror the Messaging backend permissions (Permission.DeviceToken.{Action}).
    // Granted to the User/traveler role so users manage their own devices.
    public static class DeviceToken
    {
        public const string Read   = "Permission.DeviceToken.Read";
        public const string Create = "Permission.DeviceToken.Create";
        public const string Delete = "Permission.DeviceToken.Delete";
    }

    // ── GuideDashboard ────────────────────────────────────────────────────────
    public static class GuideDashboard
    {
        public const string Read = "Permission.GuideDashboard.Read";
    }

    // ── Review (user-authored reviews + provider responses) ───────────────────
    public static class Review
    {
        public const string Read   = "Permission.Review.Read";
        public const string Create = "Permission.Review.Create";
        public const string Update = "Permission.Review.Update";
        public const string Delete = "Permission.Review.Delete";
        public const string Vote   = "Permission.Review.Vote";
    }

    // ── ReviewReply (provider responds to a review) ───────────────────────────
    public static class ReviewReply
    {
        public const string Create = "Permission.ReviewReply.Create";
    }

    // ── AccessibilityReview (accessibility-focused entity reviews) ─────────────
    // Mirror the Social backend permissions (Permission.AccessibilityReview.{Action}).
    // Distinct from Review.* (normal reviews) and AccessibilityFeature.* (place/business
    // accessibility attributes). Granted to consumer roles; ownership + the 48-hour edit
    // window are enforced server-side.
    public static class AccessibilityReview
    {
        public const string Read   = "Permission.AccessibilityReview.Read";
        public const string Create = "Permission.AccessibilityReview.Create";
        public const string Update = "Permission.AccessibilityReview.Update";
        public const string Delete = "Permission.AccessibilityReview.Delete";
    }

    // ── Creator (blog creator identity + follow) ──────────────────────────────
    // Mirror the ContentBlogs backend permissions (Permission.Creator.{Action}).
    public static class Creator
    {
        public const string Read     = "Permission.Creator.Read";
        public const string Submit   = "Permission.Creator.Submit";
        public const string Follow   = "Permission.Creator.Follow";
        public const string Unfollow = "Permission.Creator.Unfollow";
        // Update powers creator self-profile edits + avatar URL update +
        // application resubmission (PUT /creators/profile/mine, /avatar,
        // PUT /creators/applications/{id}).
        public const string Update           = "Permission.Creator.Update";
        // Delete gates voluntary self-deactivation (DELETE /creators/profile/mine).
        public const string Delete           = "Permission.Creator.Delete";
        // RedeemInvitation gates POST /creators/invitations/redeem.
        public const string RedeemInvitation = "Permission.Creator.RedeemInvitation";
    }

    // ── Blog (creator posts) ──────────────────────────────────────────────────
    // Mirror the ContentBlogs backend permissions (Permission.Blog.{Action}).
    public static class Blog
    {
        public const string Read      = "Permission.Blog.Read";
        public const string ReadOwn   = "Permission.Blog.ReadOwn";
        public const string Create    = "Permission.Blog.Create";
        public const string Update    = "Permission.Blog.Update";
        public const string DeleteOwn = "Permission.Blog.DeleteOwn";
        public const string DeleteAny = "Permission.Blog.DeleteAny";
        // Approve gates the publish/unpublish/archive transitions on the backend.
        public const string Approve   = "Permission.Blog.Approve";
        public const string Reject    = "Permission.Blog.Reject";
        public const string Remove    = "Permission.Blog.Remove";
        public const string Feature   = "Permission.Blog.Feature";
        public const string Unfeature = "Permission.Blog.Unfeature";
        public const string Submit    = "Permission.Blog.Submit";
    }

    // ── AdminBlogQueue ────────────────────────────────────────────────────────
    public static class AdminBlogQueue
    {
        public const string Read = "Permission.AdminBlogQueue.Read";
    }

    // ── BlogComment ───────────────────────────────────────────────────────────
    public static class BlogComment
    {
        public const string Read   = "Permission.BlogComment.Read";
        public const string Create = "Permission.BlogComment.Create";
    }

    // ── BlogReaction (comment reactions) ──────────────────────────────────────
    public static class BlogReaction
    {
        public const string Create = "Permission.BlogReaction.Create";
        public const string Delete = "Permission.BlogReaction.Delete";
    }

    // ── BlogTourLink ──────────────────────────────────────────────────────────
    // Mirror the ContentBlogs backend permissions (Permission.BlogTourLink.{Action}).
    public static class BlogTourLink
    {
        public const string Create = "Permission.BlogTourLink.Create";
        public const string Delete = "Permission.BlogTourLink.Delete";
    }

    // ── ProviderApplication (self-service provider onboarding) ─────────────────
    // Mirror the Accounts backend permissions (Permission.ProviderApplication.{Action}).
    public static class ProviderApplication
    {
        public const string Read     = "Permission.ProviderApplication.Read";
        public const string Register = "Permission.ProviderApplication.Register";
        public const string Create   = "Permission.ProviderApplication.Create";
        public const string Submit   = "Permission.ProviderApplication.Submit";
        // Update powers applicant self-service reapply (POST /provider/reapply) and
        // document-replace. Backend catalog gap was fixed; the permission is now grantable.
        public const string Update   = "Permission.ProviderApplication.Update";
    }

    // ── ProviderDashboard (approved-provider dashboard) ────────────────────────
    // Mirrors Permission.ProviderDashboard.Read. Granted only to approved provider
    // roles (Provider / TourGuide) — NOT to pending applicants (User role).
    public static class ProviderDashboard
    {
        public const string Read = "Permission.ProviderDashboard.Read";
    }

    // ── Tour (provider tour/listing management) ────────────────────────────────
    // Mirrors Permission.Tour.{Action}. ReadOwn/Submit/Archive are granted to approved
    // Provider/TourGuide roles (PT-0); Create/Update via the ContentManagement sweep.
    public static class Tour
    {
        public const string ReadOwn   = "Permission.Tour.ReadOwn";
        public const string Create    = "Permission.Tour.Create";
        public const string Update    = "Permission.Tour.Update";
        public const string DeleteOwn = "Permission.Tour.DeleteOwn";
        public const string Submit    = "Permission.Tour.Submit";
        public const string Archive   = "Permission.Tour.Archive";

        // ── Admin moderation (AM-1) — mirrors Permission.Tour.{Action}. Granted to
        // Admin/SuperAdmin/Owner via the ContentManagement permission sweep. ──────
        public const string ReadAny   = "Permission.Tour.ReadAny";
        public const string Approve   = "Permission.Tour.Approve";
        public const string Reject    = "Permission.Tour.Reject";
        public const string Suspend   = "Permission.Tour.Suspend";
        public const string Reinstate = "Permission.Tour.Reinstate";
    }

    // ── TourPricingTier (provider tour pricing management) ─────────────────────
    // Mirrors Permission.TourPricingTier.{Action}. Granted to approved Provider/TourGuide
    // roles via ProviderSelfPermissions + the ContentManagement sweep.
    public static class TourPricingTier
    {
        public const string Read   = "Permission.TourPricingTier.Read";
        public const string Create = "Permission.TourPricingTier.Create";
        public const string Update = "Permission.TourPricingTier.Update";
        public const string Delete = "Permission.TourPricingTier.Delete";
    }

    // ── TourSchedule (provider tour schedule management) ───────────────────────
    // Mirrors Permission.TourSchedule.{Action}. Granted to approved Provider/TourGuide
    // roles via ProviderSelfPermissions + the ContentManagement sweep.
    public static class TourSchedule
    {
        public const string Read   = "Permission.TourSchedule.Read";
        public const string Create = "Permission.TourSchedule.Create";
        public const string Update = "Permission.TourSchedule.Update";
        public const string Delete = "Permission.TourSchedule.Delete";
    }

    // ── AvailabilitySlot (provider bookable-slot management — AV-1) ─────────────
    // Mirrors Permission.AvailabilitySlot.{Action}. Granted to approved
    // Provider/TourGuide roles via ProviderSelfPermissions.
    public static class AvailabilitySlot
    {
        public const string Read   = "Permission.AvailabilitySlot.Read";
        public const string Create = "Permission.AvailabilitySlot.Create";
        public const string Update = "Permission.AvailabilitySlot.Update";
        public const string Delete = "Permission.AvailabilitySlot.Delete";
    }

    // ── AdminProviderQueue (admin provider application review) ─────────────────
    public static class AdminProviderQueue
    {
        public const string Read      = "Permission.AdminProviderQueue.Read";
        public const string Approve   = "Permission.AdminProviderQueue.Approve";
        public const string Reject    = "Permission.AdminProviderQueue.Reject";
        public const string RequestDocs = "Permission.AdminProviderQueue.RequestDocs";
        public const string Suspend   = "Permission.AdminProviderQueue.Suspend";
        public const string Reinstate = "Permission.AdminProviderQueue.Reinstate";
    }

    // ── AdminDashboard (admin analytics dashboards) ────────────────────────────
    // Mirror the Analytics backend permissions (Permission.AdminDashboard.{Action}).
    public static class AdminDashboard
    {
        public const string Read = "Permission.AdminDashboard.Read";
    }

    // ── Interaction (user interaction analytics) ───────────────────────────────
    public static class Interaction
    {
        public const string Read   = "Permission.Interaction.Read";
        public const string Create = "Permission.Interaction.Create";
    }

    // ── Recommendation (Analytics personalization consumer feed) ───────────────
    public static class Recommendation
    {
        public const string Read = "Permission.Recommendation.Read";
    }

    // ── Preference (Analytics personalization preferences) ─────────────────────
    public static class Preference
    {
        public const string Read   = "Permission.Preference.Read";
        public const string Update = "Permission.Preference.Update";
    }

    // ── AdminFinanceDashboard (admin finance/earnings dashboards) ──────────────
    // Mirror the Finance backend permission (Permission.AdminFinanceDashboard.Read).
    public static class AdminFinanceDashboard
    {
        public const string Read    = "Permission.AdminFinanceDashboard.Read";
        public const string Update  = "Permission.AdminFinanceDashboard.Update";
        public const string Approve = "Permission.AdminFinanceDashboard.Approve";
    }

    // ── CommissionRule (admin commission-rule CRUD) ─────────────────────────────
    // Mirror the Finance backend permissions (Permission.CommissionRule.{Action}).
    public static class CommissionRule
    {
        public const string Read   = "Permission.CommissionRule.Read";
        public const string Create = "Permission.CommissionRule.Create";
        public const string Update = "Permission.CommissionRule.Update";
        public const string Delete = "Permission.CommissionRule.Delete";
    }

    // ── NotificationTemplate (admin notification-template CRUD) ─────────────────
    // Mirror the Messaging backend permissions (Permission.NotificationTemplate.{Action}).
    public static class NotificationTemplate
    {
        public const string Read   = "Permission.NotificationTemplate.Read";
        public const string Create = "Permission.NotificationTemplate.Create";
        public const string Update = "Permission.NotificationTemplate.Update";
        public const string Delete = "Permission.NotificationTemplate.Delete";
    }

    // ── Notification (current user's own notification inbox) ────────────────────
    // Mirror the Messaging backend permissions (Permission.Notification.{Action}).
    public static class Notification
    {
        public const string Read   = "Permission.Notification.Read";
        public const string Update = "Permission.Notification.Update";
        public const string Delete = "Permission.Notification.Delete";
    }

    // ── AdminModerationQueue (social reports / flagged reviews / user moderation) ──
    // Mirror the Social backend permissions (Permission.AdminModerationQueue.{Action}).
    public static class AdminModerationQueue
    {
        public const string Read     = "Permission.AdminModerationQueue.Read";
        public const string Resolve  = "Permission.AdminModerationQueue.Resolve";
        public const string Warn     = "Permission.AdminModerationQueue.Warn";
        public const string Ban      = "Permission.AdminModerationQueue.Ban";
        public const string Approve  = "Permission.AdminModerationQueue.Approve";
        public const string Remove   = "Permission.AdminModerationQueue.Remove";
    }

    // ── ContentModerationLog (read-only moderation action audit trail) ──────────
    // Mirror the Social backend permission (Permission.ContentModerationLog.Read).
    public static class ContentModerationLog
    {
        public const string Read = "Permission.ContentModerationLog.Read";
    }

    // ── SupportTicket (support ticket read / close) ─────────────────────────────
    // Mirror the Messaging backend permissions (Permission.SupportTicket.{Action}).
    public static class SupportTicket
    {
        public const string Read  = "Permission.SupportTicket.Read";
        public const string Close = "Permission.SupportTicket.Close";
    }

    // ── AdminSupportQueue (support ticket assign / resolve) ─────────────────────
    // Mirror the Messaging backend permissions (Permission.AdminSupportQueue.{Action}).
    public static class AdminSupportQueue
    {
        public const string Assign  = "Permission.AdminSupportQueue.Assign";
        public const string Resolve = "Permission.AdminSupportQueue.Resolve";
    }

    // ── AdminCreatorQueue (content-creator application moderation) ──────────────
    // Mirror the ContentBlogs backend permissions (Permission.AdminCreatorQueue.{Action}).
    public static class AdminCreatorQueue
    {
        public const string Read            = "Permission.AdminCreatorQueue.Read";
        public const string Invite          = "Permission.AdminCreatorQueue.Invite";
        public const string Approve         = "Permission.AdminCreatorQueue.Approve";
        public const string Reject          = "Permission.AdminCreatorQueue.Reject";
        public const string RequestMoreInfo = "Permission.AdminCreatorQueue.RequestMoreInfo";
        public const string Suspend         = "Permission.AdminCreatorQueue.Suspend";
        public const string Reinstate       = "Permission.AdminCreatorQueue.Reinstate";
        public const string PromoteTier     = "Permission.AdminCreatorQueue.PromoteTier";
        public const string DemoteTier      = "Permission.AdminCreatorQueue.DemoteTier";
        public const string Update          = "Permission.AdminCreatorQueue.Update";
        public const string Delete          = "Permission.AdminCreatorQueue.Delete";
    }

    // ── GuideApplication (tour-guide application moderation) ───────────────────
    // Mirror the ContentTours backend permissions (Permission.GuideApplication.{Action}).
    public static class GuideApplication
    {
        public const string Read    = "Permission.GuideApplication.Read";
        public const string Approve = "Permission.GuideApplication.Approve";
        public const string Reject  = "Permission.GuideApplication.Reject";
    }

    public static class GuideOffering
    {
        public const string Read      = "Permission.GuideOffering.Read";
        public const string Create    = "Permission.GuideOffering.Create";
        public const string Update    = "Permission.GuideOffering.Update";
        public const string Delete    = "Permission.GuideOffering.Delete";
        public const string Suspend   = "Permission.GuideOffering.Suspend";
        public const string Reinstate = "Permission.GuideOffering.Reinstate";
    }

    public static class TourProposal
    {
        public const string Read   = "Permission.TourProposal.Read";
        public const string Create = "Permission.TourProposal.Create";
        public const string Submit = "Permission.TourProposal.Submit";
    }

    public static class GuideAgency
    {
        public const string Read   = "Permission.GuideAgency.Read";
        public const string Create = "Permission.GuideAgency.Create";
        public const string Update = "Permission.GuideAgency.Update";
        public const string Delete = "Permission.GuideAgency.Delete";
    }

    // ── AgencyRoster (agency-owner managing their guide roster) ─────────────────
    // Mirror the Accounts backend permissions (Permission.AgencyRoster.{Action}).
    // Granted to agency owners (TourGuide role) + Admin+; the command handlers
    // additionally enforce the agency-ownership guard, so the permission alone
    // never grants cross-agency access.
    public static class AgencyRoster
    {
        public const string Read    = "Permission.AgencyRoster.Read";
        public const string Create  = "Permission.AgencyRoster.Create";
        public const string Approve = "Permission.AgencyRoster.Approve";
        public const string Reject  = "Permission.AgencyRoster.Reject";
        public const string Delete  = "Permission.AgencyRoster.Delete";
    }

    // ── Business (place-business approval moderation) ───────────────────────────
    // Mirror the ContentPlaces backend permissions (Permission.Business.{Action}).
    public static class Business
    {
        public const string Read        = "Permission.Business.Read";
        public const string Create      = "Permission.Business.Create";
        public const string Update      = "Permission.Business.Update";
        public const string Submit      = "Permission.Business.Submit";
        public const string Approve     = "Permission.Business.Approve";
        public const string Reject      = "Permission.Business.Reject";
        public const string RequestDocs = "Permission.Business.RequestDocs";
        public const string Suspend     = "Permission.Business.Suspend";
        public const string Reinstate   = "Permission.Business.Reinstate";
        public const string Delete      = "Permission.Business.Delete";
    }

    // ── Business owner self-service (ContentPlaces module) ──────────────────────
    public static class BusinessHours
    {
        public const string Read   = "Permission.BusinessHours.Read";
        public const string Update = "Permission.BusinessHours.Update";
    }

    public static class BusinessAmenity
    {
        public const string Read   = "Permission.BusinessAmenity.Read";
        public const string Create = "Permission.BusinessAmenity.Create";
        public const string Update = "Permission.BusinessAmenity.Update";
        public const string Delete = "Permission.BusinessAmenity.Delete";
    }

    public static class BusinessStaff
    {
        public const string Read   = "Permission.BusinessStaff.Read";
        public const string Create = "Permission.BusinessStaff.Create";
        public const string Update = "Permission.BusinessStaff.Update";
        public const string Delete = "Permission.BusinessStaff.Delete";
    }

    public static class ServiceItem
    {
        public const string Read   = "Permission.ServiceItem.Read";
        public const string Create = "Permission.ServiceItem.Create";
        public const string Update = "Permission.ServiceItem.Update";
        public const string Delete = "Permission.ServiceItem.Delete";
        public const string SoftDelete = "Permission.ServiceItem.SoftDelete";
    }

    public static class AccessibilityFeature
    {
        public const string Read   = "Permission.AccessibilityFeature.Read";
        public const string Update = "Permission.AccessibilityFeature.Update";
    }

    // ── AuditLog (admin audit trail) ───────────────────────────────────────────
    public static class AuditLog
    {
        public const string Read   = "Permission.AuditLog.Read";
        public const string Redact = "Permission.AuditLog.Redact";
        public const string Export = "Permission.AuditLog.Export";
    }

    // ── Recommendations engine (Analytics admin) ──────────────────────────────
    public static class Batch
    {
        public const string Read    = "Permission.Batch.Read";
        public const string Refresh = "Permission.Batch.Refresh";
    }

    public static class BoostPackage
    {
        public const string Create = "Permission.BoostPackage.Create";
        public const string Delete = "Permission.BoostPackage.Delete";
    }

    public static class EditorialPin
    {
        public const string Create = "Permission.EditorialPin.Create";
        public const string Delete = "Permission.EditorialPin.Delete";
    }
}
