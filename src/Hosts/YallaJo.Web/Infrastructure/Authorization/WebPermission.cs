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

    // ── Blog ──────────────────────────────────────────────────────────────────
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
        public const string ReadOwn = "Permission.Tour.ReadOwn";
        public const string Create  = "Permission.Tour.Create";
        public const string Update  = "Permission.Tour.Update";
        public const string Submit  = "Permission.Tour.Submit";
        public const string Archive = "Permission.Tour.Archive";
    }

    // ── TourPricingTier (provider tour pricing management) ─────────────────────
    // Mirrors Permission.TourPricingTier.{Action}. Granted to approved Provider/TourGuide
    // roles via ProviderSelfPermissions + the ContentManagement sweep.
    public static class TourPricingTier
    {
        public const string Create = "Permission.TourPricingTier.Create";
        public const string Update = "Permission.TourPricingTier.Update";
        public const string Delete = "Permission.TourPricingTier.Delete";
    }

    // ── TourSchedule (provider tour schedule management) ───────────────────────
    // Mirrors Permission.TourSchedule.{Action}. Granted to approved Provider/TourGuide
    // roles via ProviderSelfPermissions + the ContentManagement sweep.
    public static class TourSchedule
    {
        public const string Create = "Permission.TourSchedule.Create";
        public const string Update = "Permission.TourSchedule.Update";
        public const string Delete = "Permission.TourSchedule.Delete";
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
}
