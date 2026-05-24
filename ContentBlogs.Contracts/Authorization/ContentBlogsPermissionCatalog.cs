using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentBlogs.Contracts.Authorization
{
    public sealed class ContentBlogsPermissionCatalog : IPermissionCatalog
    {
        public string ModuleName => "ContentBlogs";

        public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
        [
         // ── Blog ────────────────────────────────────────────────────────
        new(ContentBlogsFeatures.Blog, AppAction.Read,   PermissionGroup.ContentManagement, "View blogs"),
        new(ContentBlogsFeatures.Blog, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog"),
        new(ContentBlogsFeatures.Blog, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog"),
        new(ContentBlogsFeatures.Blog, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog"),
        new(ContentBlogsFeatures.Blog, AppAction.Approve, PermissionGroup.ContentManagement, "Approve a blog"),

        // ── BlogComment ───────────────────────────────────────────────────
        new(ContentBlogsFeatures.BlogComment, AppAction.Read,   PermissionGroup.ContentManagement, "View blog comments"),
        new(ContentBlogsFeatures.BlogComment, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog comment"),
        new(ContentBlogsFeatures.BlogComment, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog comment"),
        new(ContentBlogsFeatures.BlogComment, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog comment"),

        // ── BlogReaction ───────────────────────────────────────────────────
      //  new(ContentBlogFeatures.BlogReaction, AppAction.Read,   PermissionGroup.ContentManagement, "View blog reactions"),
        new(ContentBlogsFeatures.BlogReaction, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog reaction"),
        //new(ContentBlogFeatures.BlogReaction, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog reaction"),
        new(ContentBlogsFeatures.BlogReaction, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog reaction"),

         // ── BlogTourLink ───────────────────────────────────────────────────
      //  new(ContentBlogFeatures.BlogTourLink, AppAction.Read,   PermissionGroup.ContentManagement, "View blog tour links"),
        new(ContentBlogsFeatures.BlogTourLink, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog tour link"),
      //  new(ContentBlogFeatures.BlogTourLink, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog tour link"),
        new(ContentBlogsFeatures.BlogTourLink, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog tour link "),

        // ── Creator self-service (Wave-7) ──────────────────────────────
        new(ContentBlogsFeatures.Creator, AppAction.Submit, PermissionGroup.SystemAccess, "Submit creator application"),
        new(ContentBlogsFeatures.Creator, AppAction.Read, PermissionGroup.SystemAccess, "Read own creator application/profile"),
        new(ContentBlogsFeatures.Creator, AppAction.Update, PermissionGroup.SystemAccess, "Update own creator profile"),

        // ── Creator social (Wave-7) ──────────────────────────────────
        new(ContentBlogsFeatures.Creator, AppAction.Follow, PermissionGroup.SystemAccess, "Follow a content creator"),
        new(ContentBlogsFeatures.Creator, AppAction.Unfollow, PermissionGroup.SystemAccess, "Unfollow a content creator"),
        new(ContentBlogsFeatures.Creator, AppAction.RedeemInvitation, PermissionGroup.SystemAccess, "Redeem a creator invitation"),

        // ── Article authoring by creator ───────────────────────────────
        new($"{ContentBlogsFeatures.Creator}.Article", AppAction.Create, PermissionGroup.SystemAccess, "Create blog article as creator"),
        new($"{ContentBlogsFeatures.Creator}.Article", AppAction.Update, PermissionGroup.SystemAccess, "Update/delete own article"),

        // ── Admin creator queue (Wave-7) ───────────────────────────────
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Read, PermissionGroup.ModerationTools, "View creator application queue"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Invite, PermissionGroup.ModerationTools, "Send creator invitation"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Approve, PermissionGroup.ModerationTools, "Approve creator application"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Reject, PermissionGroup.ModerationTools, "Reject creator application"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.RequestMoreInfo, PermissionGroup.ModerationTools, "Request more info from creator"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Suspend, PermissionGroup.ModerationTools, "Suspend creator profile"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Reinstate, PermissionGroup.ModerationTools, "Reinstate suspended creator"),

        // ── Creator Posts self-service (Wave-8) ────────────────────────
        new(ContentBlogsFeatures.CreatorPost, AppAction.Read, PermissionGroup.SystemAccess, "View own creator posts"),
        new(ContentBlogsFeatures.CreatorPost, AppAction.Create, PermissionGroup.SystemAccess, "Create a creator post"),
        new(ContentBlogsFeatures.CreatorPost, AppAction.Update, PermissionGroup.SystemAccess, "Update own creator post"),
        new(ContentBlogsFeatures.CreatorPost, AppAction.Delete, PermissionGroup.SystemAccess, "Delete own draft/rejected creator post"),
        new(ContentBlogsFeatures.CreatorPost, AppAction.Submit, PermissionGroup.SystemAccess, "Submit creator post for review / publish directly"),

        // ── Admin Post Moderation (Wave-8) ─────────────────────────────
        new(ContentBlogsFeatures.AdminPostModeration, AppAction.Read, PermissionGroup.ModerationTools, "View post moderation queue"),
        new(ContentBlogsFeatures.AdminPostModeration, AppAction.Approve, PermissionGroup.ModerationTools, "Approve submitted creator post"),
        new(ContentBlogsFeatures.AdminPostModeration, AppAction.Reject, PermissionGroup.ModerationTools, "Reject submitted creator post"),
        new(ContentBlogsFeatures.AdminPostModeration, AppAction.Feature, PermissionGroup.ModerationTools, "Feature a published creator post"),
        new(ContentBlogsFeatures.AdminPostModeration, AppAction.Remove, PermissionGroup.ModerationTools, "Remove a published creator post"),
        new(ContentBlogsFeatures.AdminPostModeration, AppAction.HidePost, PermissionGroup.ModerationTools, "Hide a published creator post"),
        new(ContentBlogsFeatures.AdminPostModeration, AppAction.UnhidePost, PermissionGroup.ModerationTools, "Unhide a hidden creator post"),

        // ── Admin Tier Management (Wave-8) ─────────────────────────────
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.PromoteTier, PermissionGroup.ModerationTools, "Promote creator trust tier"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.DemoteTier, PermissionGroup.ModerationTools, "Demote creator trust tier"),
    ];
    }

}
