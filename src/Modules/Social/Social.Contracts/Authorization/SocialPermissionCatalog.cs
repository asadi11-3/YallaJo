using YallaJo.SharedKernel.Application.Authorization;

namespace Social.Contracts.Authorization;

/// <summary>21 Social-module permissions across 7 features.</summary>
public sealed class SocialPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Social";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── Review (5) ──────────────────────────────────────────────────────
        new(SocialFeatures.Review, AppAction.Create, PermissionGroup.ContentManagement,
            "Submit a new review for a tour, place, or business"),
        new(SocialFeatures.Review, AppAction.Read, PermissionGroup.ContentManagement,
            "Read published reviews"),
        new(SocialFeatures.Review, AppAction.Update, PermissionGroup.ContentManagement,
            "Edit own review within the 48-hour edit window"),
        new(SocialFeatures.Review, AppAction.Delete, PermissionGroup.ContentManagement,
            "Soft-delete own review"),
        new(SocialFeatures.Review, AppAction.Vote, PermissionGroup.ContentManagement,
            "Mark a review as helpful"),

        // ── ReviewReply (3) ─────────────────────────────────────────────────
        new(SocialFeatures.ReviewReply, AppAction.Create, PermissionGroup.ContentManagement,
            "Post a provider reply to a review"),
        new(SocialFeatures.ReviewReply, AppAction.Update, PermissionGroup.ContentManagement,
            "Edit own provider reply"),
        new(SocialFeatures.ReviewReply, AppAction.Delete, PermissionGroup.ContentManagement,
            "Delete own provider reply"),

        // ── Favorite (3) ────────────────────────────────────────────────────
        new(SocialFeatures.Favorite, AppAction.Create, PermissionGroup.ContentManagement,
            "Add an entity to favorites (max 500 per user)"),
        new(SocialFeatures.Favorite, AppAction.Read, PermissionGroup.ContentManagement,
            "List own favorites"),
        new(SocialFeatures.Favorite, AppAction.Delete, PermissionGroup.ContentManagement,
            "Remove an entity from favorites"),

        // ── Report (2) ──────────────────────────────────────────────────────
        new(SocialFeatures.Report, AppAction.Create, PermissionGroup.ContentManagement,
            "Submit an abuse / policy-violation report"),
        new(SocialFeatures.Report, AppAction.Read, PermissionGroup.ModerationTools,
            "View submitted reports (admin)"),

        // ── ContentModerationLog (1) ─────────────────────────────────────────
        new(SocialFeatures.ContentModerationLog, AppAction.Read, PermissionGroup.ModerationTools,
            "View the admin moderation action log"),

        // ── AdminModerationQueue (6) ─────────────────────────────────────────
        new(SocialFeatures.AdminModerationQueue, AppAction.Read, PermissionGroup.ModerationTools,
            "View the moderation queue of flagged content"),
        new(SocialFeatures.AdminModerationQueue, AppAction.Approve, PermissionGroup.ModerationTools,
            "Approve / restore auto-hidden or flagged content"),
        new(SocialFeatures.AdminModerationQueue, AppAction.Remove, PermissionGroup.ModerationTools,
            "Remove content from the platform (admin hard-delete)"),
        new(SocialFeatures.AdminModerationQueue, AppAction.Resolve, PermissionGroup.ModerationTools,
            "Mark a report as resolved"),
        new(SocialFeatures.AdminModerationQueue, AppAction.Warn, PermissionGroup.ModerationTools,
            "Issue a warning to a content author"),
        new(SocialFeatures.AdminModerationQueue, AppAction.Ban, PermissionGroup.ModerationTools,
            "Ban a user from the platform"),

        new(SocialFeatures.UserModeration, AppAction.Create, PermissionGroup.ModerationTools,
            "Create a user warning or temporary ban record"),
    ];
}
