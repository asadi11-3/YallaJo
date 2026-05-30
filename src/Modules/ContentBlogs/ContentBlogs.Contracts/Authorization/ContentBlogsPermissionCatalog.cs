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
        new(ContentBlogsFeatures.Blog, AppAction.Read,    PermissionGroup.ContentManagement, "View blogs"),
        new(ContentBlogsFeatures.Blog, AppAction.ReadOwn, PermissionGroup.SystemAccess, "View own blogs (creator)"),
        new(ContentBlogsFeatures.Blog, AppAction.Create,  PermissionGroup.ContentManagement, "Create a blog"),
        new(ContentBlogsFeatures.Blog, AppAction.Update,  PermissionGroup.ContentManagement, "Update a blog"),
        // P1 DeleteOwn/DeleteAny split (2026-05-30): replaces Blog.Delete.
        new(ContentBlogsFeatures.Blog, AppAction.DeleteOwn, PermissionGroup.ContentManagement, "Delete own blog"),
        new(ContentBlogsFeatures.Blog, AppAction.DeleteAny, PermissionGroup.ContentManagement, "Delete any blog (admin)"),
        new(ContentBlogsFeatures.Blog, AppAction.Submit,  PermissionGroup.SystemAccess, "Submit blog for review (creator)"),
        new(ContentBlogsFeatures.Blog, AppAction.Approve, PermissionGroup.ModerationTools, "Approve a blog (admin)"),
        new(ContentBlogsFeatures.Blog, AppAction.Reject,  PermissionGroup.ModerationTools, "Reject a blog (admin)"),
        new(ContentBlogsFeatures.Blog, AppAction.Remove,  PermissionGroup.ModerationTools, "Remove a published blog (admin)"),
        new(ContentBlogsFeatures.Blog, AppAction.Feature, PermissionGroup.ModerationTools, "Feature a blog (admin)"),
        new(ContentBlogsFeatures.Blog, AppAction.Unfeature, PermissionGroup.ModerationTools, "Unfeature a blog (admin)"),
        new(ContentBlogsFeatures.AdminBlogQueue, AppAction.Read, PermissionGroup.ModerationTools, "View blog moderation queue"),

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

        // CreatorPost + AdminPostModeration permissions removed — merged into Blog (Wave-8 → Blog).

        // ── Admin Tier Management (Wave-8) ─────────────────────────────
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.PromoteTier, PermissionGroup.ModerationTools, "Promote creator trust tier"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.DemoteTier, PermissionGroup.ModerationTools, "Demote creator trust tier"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Update, PermissionGroup.ModerationTools, "Admin — edit creator profile"),
        new(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Delete, PermissionGroup.ModerationTools, "Admin — soft-delete creator profile"),

        // ── Creator self-management ────────────────────────────────────
        new(ContentBlogsFeatures.Creator, AppAction.Delete, PermissionGroup.SystemAccess, "Self-deactivate creator account"),
    ];
    }

}
