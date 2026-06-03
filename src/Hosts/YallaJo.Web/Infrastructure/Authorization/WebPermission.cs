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

    // ── Place ─────────────────────────────────────────────────────────────────
    public static class Place
    {
        public const string Read   = "Permission.Place.Read";
        public const string Create = "Permission.Place.Create";
        public const string Update = "Permission.Place.Update";
        public const string Delete = "Permission.Place.Delete";
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

    // ── Tour (provider listings) ──────────────────────────────────────────────
    public static class Tour
    {
        public const string Create    = "Permission.Tour.Create";
        public const string Update    = "Permission.Tour.Update";
        public const string ReadOwn   = "Permission.Tour.ReadOwn";
        public const string DeleteOwn = "Permission.Tour.DeleteOwn";
        public const string Submit    = "Permission.Tour.Submit";
        public const string Archive   = "Permission.Tour.Archive";
    }

    // ── TourSchedule ──────────────────────────────────────────────────────────
    public static class TourSchedule
    {
        public const string Read   = "Permission.TourSchedule.Read";
        public const string Create = "Permission.TourSchedule.Create";
        public const string Update = "Permission.TourSchedule.Update";
        public const string Delete = "Permission.TourSchedule.Delete";
    }

    // ── TourPricingTier ───────────────────────────────────────────────────────
    public static class TourPricingTier
    {
        public const string Read   = "Permission.TourPricingTier.Read";
        public const string Create = "Permission.TourPricingTier.Create";
        public const string Update = "Permission.TourPricingTier.Update";
        public const string Delete = "Permission.TourPricingTier.Delete";
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

    // ── TourGuideProfile (guide self profile) ─────────────────────────────────
    public static class TourGuideProfile
    {
        public const string Read      = "Permission.TourGuideProfile.Read";
        public const string DeleteOwn = "Permission.TourGuideProfile.DeleteOwn";
    }

    // ── TourBooking (provider-side booking lifecycle) ─────────────────────────
    public static class TourBooking
    {
        public const string ReadOwn  = "Permission.TourBooking.ReadOwn";
        public const string Create   = "Permission.TourBooking.Create";
        public const string Cancel   = "Permission.TourBooking.Cancel";
        public const string Confirm  = "Permission.TourBooking.Confirm";
        public const string Complete = "Permission.TourBooking.Complete";
        public const string Reject   = "Permission.TourBooking.Reject";
    }

    // ── JoinRequest (group-booking join queue) ────────────────────────────────
    public static class JoinRequest
    {
        public const string ReadOwn = "Permission.JoinRequest.ReadOwn";
        public const string Create  = "Permission.JoinRequest.Create";
        public const string Approve = "Permission.JoinRequest.Approve";
        public const string Reject  = "Permission.JoinRequest.Reject";
    }

    // ── Payout (provider earnings/payouts) ────────────────────────────────────
    public static class Payout
    {
        public const string Read = "Permission.Payout.Read";
    }

    // ── Refund (provider disputes) ────────────────────────────────────────────
    public static class Refund
    {
        public const string Read   = "Permission.Refund.Read";
        public const string Create = "Permission.Refund.Create";
    }

    // ── ProviderDashboard ─────────────────────────────────────────────────────
    public static class ProviderDashboard
    {
        public const string Read = "Permission.ProviderDashboard.Read";
    }

    // ── GuideDashboard ────────────────────────────────────────────────────────
    public static class GuideDashboard
    {
        public const string Read = "Permission.GuideDashboard.Read";
    }

    // ── Review (provider responses) ───────────────────────────────────────────
    public static class Review
    {
        public const string Read   = "Permission.Review.Read";
        public const string Create = "Permission.Review.Create";
    }

    // ── ReviewReply (provider responds to a review) ───────────────────────────
    public static class ReviewReply
    {
        public const string Create = "Permission.ReviewReply.Create";
    }

    // ── Creator (blog creator identity + follow) ──────────────────────────────
    public static class Creator
    {
        public const string Read     = "Permission.Creator.Read";
        public const string Submit   = "Permission.Creator.Submit";
        public const string Follow   = "Permission.Creator.Follow";
        public const string Unfollow = "Permission.Creator.Unfollow";
    }

    // ── Blog (creator posts) ──────────────────────────────────────────────────
    public static class Blog
    {
        public const string Create    = "Permission.Blog.Create";
        public const string Update    = "Permission.Blog.Update";
        public const string DeleteOwn = "Permission.Blog.DeleteOwn";
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
}
