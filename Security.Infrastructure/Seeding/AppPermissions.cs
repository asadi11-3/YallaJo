using Security.Contracts.Authorization;

namespace Security.Infrastructure.Seeding;

internal static class AppPermissions
{
    public static IReadOnlyList<AppPermission> All { get; } = new List<AppPermission>
    {
        // ── SystemAccess ──────────────────────────────────────────────────────
        new(AppFeatures.Role,        AppAction.Read,       AppRoleGroup.SystemAccess,    "View roles"),
        new(AppFeatures.Role,        AppAction.Create,     AppRoleGroup.SystemAccess,    "Create a new role"),
        new(AppFeatures.Role,        AppAction.Update,     AppRoleGroup.SystemAccess,    "Edit a role"),
        new(AppFeatures.Role,        AppAction.Delete,     AppRoleGroup.SystemAccess,    "Delete a role"),

        new(AppFeatures.UserRole,    AppAction.Create,     AppRoleGroup.SystemAccess,    "Assign a role to a user"),
        new(AppFeatures.UserRole,    AppAction.Delete,     AppRoleGroup.SystemAccess,    "Remove a role from a user"),

        new(AppFeatures.RoleClaim,   AppAction.Read,       AppRoleGroup.SystemAccess,    "View role claims"),
        new(AppFeatures.RoleClaim,   AppAction.Create,     AppRoleGroup.SystemAccess,    "Add a claim to a role"),
        new(AppFeatures.RoleClaim,   AppAction.Delete,     AppRoleGroup.SystemAccess,    "Remove a claim from a role"),

        new(AppFeatures.User,        AppAction.Read,       AppRoleGroup.SystemAccess,    "View any user"),
        new(AppFeatures.User,        AppAction.UpdateAny,  AppRoleGroup.SystemAccess,    "Update any user"),
        new(AppFeatures.User,        AppAction.DeleteAny,  AppRoleGroup.SystemAccess,    "Hard-delete any user"),
        new(AppFeatures.User,        AppAction.SoftDelete, AppRoleGroup.SystemAccess,    "Soft-delete any user"),
        new(AppFeatures.User,        AppAction.UpdateSelf, AppRoleGroup.SystemAccess,    "Update own profile"),

        new(AppFeatures.System,      AppAction.Update,     AppRoleGroup.SystemAccess,    "Manage system settings"),

        // ── ContentManagement ────────────────────────────────────────────────
        new(AppFeatures.Category,            AppAction.Read,   AppRoleGroup.ContentManagement, "View categories"),
        new(AppFeatures.Category,            AppAction.Create, AppRoleGroup.ContentManagement, "Create a category"),
        new(AppFeatures.Category,            AppAction.Update, AppRoleGroup.ContentManagement, "Update a category"),
        new(AppFeatures.Category,            AppAction.Delete, AppRoleGroup.ContentManagement, "Delete a category"),

        new(AppFeatures.CategoryTranslation, AppAction.Read,   AppRoleGroup.ContentManagement, "View category translations"),
        new(AppFeatures.CategoryTranslation, AppAction.Create, AppRoleGroup.ContentManagement, "Create a category translation"),
        new(AppFeatures.CategoryTranslation, AppAction.Update, AppRoleGroup.ContentManagement, "Update a category translation"),
        new(AppFeatures.CategoryTranslation, AppAction.Delete, AppRoleGroup.ContentManagement, "Delete a category translation"),

        new(AppFeatures.Specialization,      AppAction.Read,   AppRoleGroup.ContentManagement, "View specializations"),
        new(AppFeatures.Specialization,      AppAction.Create, AppRoleGroup.ContentManagement, "Create a specialization"),
        new(AppFeatures.Specialization,      AppAction.Update, AppRoleGroup.ContentManagement, "Update a specialization"),
        new(AppFeatures.Specialization,      AppAction.Delete, AppRoleGroup.ContentManagement, "Delete a specialization"),

        new(AppFeatures.Tag,                 AppAction.Read,   AppRoleGroup.ContentManagement, "View tags"),
        new(AppFeatures.Tag,                 AppAction.Create, AppRoleGroup.ContentManagement, "Create a tag"),
        new(AppFeatures.Tag,                 AppAction.Update, AppRoleGroup.ContentManagement, "Update a tag"),
        new(AppFeatures.Tag,                 AppAction.Delete, AppRoleGroup.ContentManagement, "Delete a tag"),

        new(AppFeatures.EntityCategory,      AppAction.Read,   AppRoleGroup.ContentManagement, "View entity-category assignments"),
        new(AppFeatures.EntityCategory,      AppAction.Create, AppRoleGroup.ContentManagement, "Assign category to entity"),
        new(AppFeatures.EntityCategory,      AppAction.Delete, AppRoleGroup.ContentManagement, "Remove category from entity"),

        new(AppFeatures.EntityImage,         AppAction.Read,   AppRoleGroup.ContentManagement, "View entity images"),
        new(AppFeatures.EntityImage,         AppAction.Create, AppRoleGroup.ContentManagement, "Upload an entity image"),
        new(AppFeatures.EntityImage,         AppAction.Update, AppRoleGroup.ContentManagement, "Update entity image details"),
        new(AppFeatures.EntityImage,         AppAction.Delete, AppRoleGroup.ContentManagement, "Delete an entity image"),

        new(AppFeatures.EntityTag,           AppAction.Read,   AppRoleGroup.ContentManagement, "View entity-tag assignments"),
        new(AppFeatures.EntityTag,           AppAction.Create, AppRoleGroup.ContentManagement, "Assign tag to entity"),
        new(AppFeatures.EntityTag,           AppAction.Delete, AppRoleGroup.ContentManagement, "Remove tag from entity"),

        new(AppFeatures.TranslationCache,    AppAction.Read,   AppRoleGroup.ContentManagement, "View translation cache entries"),
        new(AppFeatures.TranslationCache,    AppAction.Create, AppRoleGroup.ContentManagement, "Create a translation cache entry"),
        new(AppFeatures.TranslationCache,    AppAction.Update, AppRoleGroup.ContentManagement, "Update a translation cache entry"),
        new(AppFeatures.TranslationCache,    AppAction.Delete, AppRoleGroup.ContentManagement, "Delete a translation cache entry"),

        new(AppFeatures.Language,            AppAction.Read,   AppRoleGroup.ContentManagement, "View languages"),
        new(AppFeatures.Language,            AppAction.Create, AppRoleGroup.ContentManagement, "Create a language"),
        new(AppFeatures.Language,            AppAction.Update, AppRoleGroup.ContentManagement, "Update a language"),
        new(AppFeatures.Language,            AppAction.Delete, AppRoleGroup.ContentManagement, "Delete a language"),

        new(AppFeatures.Attachment,          AppAction.Read,   AppRoleGroup.ContentManagement, "View attachments"),
        new(AppFeatures.Attachment,          AppAction.Create, AppRoleGroup.ContentManagement, "Upload an attachment"),
        new(AppFeatures.Attachment,          AppAction.Update, AppRoleGroup.ContentManagement, "Update attachment details"),
        new(AppFeatures.Attachment,          AppAction.Delete, AppRoleGroup.ContentManagement, "Delete an attachment"),

        new(AppFeatures.Place, AppAction.Read,    AppRoleGroup.ContentManagement, "View places"),
        new(AppFeatures.Place, AppAction.Create,  AppRoleGroup.ContentManagement, "Create a place"),
        new(AppFeatures.Place, AppAction.Update,  AppRoleGroup.ContentManagement, "Update place details"),
        new(AppFeatures.Place, AppAction.Delete,  AppRoleGroup.ContentManagement, "Delete a place"),

        // *── BusinessStaff ─────────────────────────────────────────────
        new(AppFeatures.BusinessStaff, AppAction.Read,   AppRoleGroup.ContentManagement, "View business staff"),
        new(AppFeatures.BusinessStaff, AppAction.Create, AppRoleGroup.ContentManagement, "Add business staff"),
        new(AppFeatures.BusinessStaff, AppAction.Delete, AppRoleGroup.ContentManagement, "Remove business staff"),

        // *── BusinessAmenity ──────────────────────────────────────────
        new(AppFeatures.BusinessAmenity, AppAction.Read,   AppRoleGroup.ContentManagement, "View business amenities"),
        new(AppFeatures.BusinessAmenity, AppAction.Create, AppRoleGroup.ContentManagement, "Add business amenity"),
        new(AppFeatures.BusinessAmenity, AppAction.Delete, AppRoleGroup.ContentManagement, "Remove business amenity"),

        // *── AccessibilityFeature ──────────────────────────────────────
        new(AppFeatures.AccessibilityFeature, AppAction.Read,   AppRoleGroup.ContentManagement, "View accessibility features"),
        new(AppFeatures.AccessibilityFeature, AppAction.Update, AppRoleGroup.ContentManagement, "Update accessibility features"),

        // *── ServiceItem ──────────────────────────────────────────────
        new(AppFeatures.ServiceItem, AppAction.Read,       AppRoleGroup.ContentManagement, "View service items"),
        new(AppFeatures.ServiceItem, AppAction.Create,     AppRoleGroup.ContentManagement, "Create a service item"),
        new(AppFeatures.ServiceItem, AppAction.Update,     AppRoleGroup.ContentManagement, "Update a service item"),
        new(AppFeatures.ServiceItem, AppAction.SoftDelete, AppRoleGroup.ContentManagement, "Soft-delete a service item"),
    }.AsReadOnly();

    public static IReadOnlyList<string> GetPermissionsForRole(string roleName) =>
        roleName switch
        {
            AppRoles.Owner =>
                All.Select(p => p.Name).ToList(),

            AppRoles.SuperAdmin =>
                All.Where(p => !(p.Feature == AppFeatures.System && p.Action == AppAction.Update))
                   .Select(p => p.Name).ToList(),

            AppRoles.Admin =>
                All.Where(p => !(p.Feature == AppFeatures.System && p.Action == AppAction.Update)
                            && !(p.Feature == AppFeatures.User && p.Action == AppAction.DeleteAny))
                   .Select(p => p.Name).ToList(),

            AppRoles.User =>
                All.Where(p => (p.Feature == AppFeatures.User && p.Action == AppAction.UpdateSelf) || p.IsGuest)
                   .Select(p => p.Name).ToList(),

            AppRoles.TourGuide =>
                All.Where(p => p.Group == AppRoleGroup.ContentManagement
                            && (p.Action == AppAction.Read || p.Action == AppAction.Create))
                   .Select(p => p.Name).ToList(),

            AppRoles.Guest =>
                All.Where(p => p.IsGuest)
                   .Select(p => p.Name).ToList(),

            _ => Array.Empty<string>()
        };
}
