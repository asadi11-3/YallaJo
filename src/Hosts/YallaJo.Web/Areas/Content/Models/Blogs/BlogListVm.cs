namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class BlogListVm
{
    public IReadOnlyList<BlogCardVm> Blogs { get; init; } = [];
    public IReadOnlyList<BlogNicheVm> Niches { get; init; } = [];
    public IReadOnlyList<CategoryBadgeVm> Categories { get; init; } = [];
    public BlogPagerVm Pager { get; init; } = new();

    public bool HasBlogs => Blogs.Count > 0;
}
