using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentBlogs.Contracts.Authorization
{
    public sealed class ContentBlogPermissionCatalog : IPermissionCatalog
    {
        public string ModuleName => "ContentBlogs";

        public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
        [
         // ── Blog ────────────────────────────────────────────────────────
        new(ContentBlogFeatures.Blog, AppAction.Read,   PermissionGroup.ContentManagement, "View blogs"),
        new(ContentBlogFeatures.Blog, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog"),
        new(ContentBlogFeatures.Blog, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog"),
        new(ContentBlogFeatures.Blog, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog"),
        new(ContentBlogFeatures.Blog, AppAction.Approve, PermissionGroup.ContentManagement, "Approve a blog"),

        // ── BlogComment ───────────────────────────────────────────────────
        new(ContentBlogFeatures.BlogComment, AppAction.Read,   PermissionGroup.ContentManagement, "View blog comments"),
        new(ContentBlogFeatures.BlogComment, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog comment"),
        new(ContentBlogFeatures.BlogComment, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog comment"),
        new(ContentBlogFeatures.BlogComment, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog comment"),
        new (ContentBlogFeatures.BlogComment, AppAction.Manage, PermissionGroup.ContentManagement, "Manage a blog comment"),

        // ── BlogReaction ───────────────────────────────────────────────────
      //  new(ContentBlogFeatures.BlogReaction, AppAction.Read,   PermissionGroup.ContentManagement, "View blog reactions"),
        new(ContentBlogFeatures.BlogReaction, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog reaction"),
        //new(ContentBlogFeatures.BlogReaction, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog reaction"),
        new(ContentBlogFeatures.BlogReaction, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog reaction"),

         // ── BlogTourLink ───────────────────────────────────────────────────
      //  new(ContentBlogFeatures.BlogTourLink, AppAction.Read,   PermissionGroup.ContentManagement, "View blog tour links"),
        new(ContentBlogFeatures.BlogTourLink, AppAction.Create, PermissionGroup.ContentManagement, "Create a blog tour link"),
      //  new(ContentBlogFeatures.BlogTourLink, AppAction.Update, PermissionGroup.ContentManagement, "Update a blog tour link"),
        new(ContentBlogFeatures.BlogTourLink, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a blog tour link "),
    ];
    }

}
