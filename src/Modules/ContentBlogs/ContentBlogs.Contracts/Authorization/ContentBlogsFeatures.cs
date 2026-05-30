using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentBlogs.Contracts.Authorization
{
    public static class ContentBlogsFeatures
    {
        public const string Blog = nameof(Blog);
        public const string BlogComment = nameof(BlogComment);
        public const string BlogReaction = nameof(BlogReaction);
        public const string BlogTourLink = nameof(BlogTourLink);

        // ── Creator (Wave-7) ───────────────────────────────────────────
        public const string Creator = nameof(Creator);
        public const string AdminCreatorQueue = nameof(AdminCreatorQueue);

        // ── Admin Blog Queue ────────────────────────────────────────────
        public const string AdminBlogQueue = nameof(AdminBlogQueue);

        // CreatorPost + AdminPostModeration removed — merged into Blog (Wave-8 → Blog).
    }
}
