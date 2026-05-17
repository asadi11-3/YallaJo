using YallaJo.SharedKernel.Application.Authorization;

namespace Social.Contracts.Authorization;

public sealed class SocialPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Social";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // Review (6)
        new(SocialFeatures.Review, AppAction.Create, PermissionGroup.ContentManagement, "Create a review"),
        new(SocialFeatures.Review, AppAction.Read, PermissionGroup.ContentManagement, "View reviews"),
        new(SocialFeatures.Review, AppAction.UpdateSelf, PermissionGroup.ContentManagement, "Edit own review"),
        new(SocialFeatures.Review, AppAction.DeleteAny, PermissionGroup.ModerationTools, "Delete any review"),
        new(SocialFeatures.Review, AppAction.SoftDelete, PermissionGroup.ContentManagement, "Soft delete own review"),
        new(SocialFeatures.Review, AppAction.Approve, PermissionGroup.ModerationTools, "Mark review as verified"),

        // Favorite (3)
        new(SocialFeatures.Favorite, AppAction.Create, PermissionGroup.ContentManagement, "Add favorite"),
        new(SocialFeatures.Favorite, AppAction.Read, PermissionGroup.ContentManagement, "View own favorites"),
        new(SocialFeatures.Favorite, AppAction.Delete, PermissionGroup.ContentManagement, "Remove favorite"),

        // Report (4)
        new(SocialFeatures.Report, AppAction.Create, PermissionGroup.ContentManagement, "Report content"),
        new(SocialFeatures.Report, AppAction.ReadAny, PermissionGroup.ModerationTools, "View all reports"),
        new(SocialFeatures.Report, AppAction.Resolve, PermissionGroup.ModerationTools, "Resolve report"),
        new(SocialFeatures.Report, AppAction.Reject, PermissionGroup.ModerationTools, "Reject report"),

        // Moderation (3)
        new(SocialFeatures.Moderation, AppAction.ReadAny, PermissionGroup.ModerationTools, "View moderation log"),
        new(SocialFeatures.Moderation, AppAction.Suspend, PermissionGroup.ModerationTools, "Hide content"),
        new(SocialFeatures.Moderation, AppAction.Reinstate, PermissionGroup.ModerationTools, "Restore content"),

        // AccessibilityReview (2)
        new(SocialFeatures.AccessibilityReview, AppAction.Create, PermissionGroup.ContentManagement, "Create accessibility review"),
        new(SocialFeatures.AccessibilityReview, AppAction.Read, PermissionGroup.ContentManagement, "View accessibility reviews"),

        // SocialAdmin (1)
        new(SocialFeatures.SocialAdmin, AppAction.Export, PermissionGroup.ModerationTools, "Export moderation data"),
    ];
}
