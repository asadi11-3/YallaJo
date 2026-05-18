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
        new (ContentBlogsFeatures.BlogComment, AppAction.Manage, PermissionGroup.ContentManagement, "Manage a blog comment"),

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
    ];
    }

}
