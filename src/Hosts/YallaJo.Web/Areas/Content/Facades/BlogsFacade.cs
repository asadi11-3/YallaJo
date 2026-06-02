using YallaJo.Web.Areas.Content.ApiClients;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.Facades;

public sealed class BlogsFacade
{
    private const int CommentPageSize = 50;
    private const int PopularTagCount = 12;

    private readonly BlogsApiClient _blogs;
    private readonly CategoriesApiClient _categories;
    private readonly TagsApiClient _tags;
    private readonly IApiAssetUrlResolver _assetResolver;

    public BlogsFacade(
        BlogsApiClient blogs,
        CategoriesApiClient categories,
        TagsApiClient tags,
        IApiAssetUrlResolver assetResolver)
    {
        _blogs = blogs;
        _categories = categories;
        _tags = tags;
        _assetResolver = assetResolver;
    }

    public async Task<ApiResult<BlogListVm>> GetListAsync(
        int page, int pageSize, string? search, bool? isFeatured, CancellationToken ct = default)
    {
        var blogsTask = _blogs.ListBlogsAsync(page, pageSize, search, isFeatured, ct);
        var nichesTask = _blogs.ListCreatorNichesAsync(ct);
        var categoriesTask = _categories.ListCategoriesAsync(ct);

        await Task.WhenAll(blogsTask, nichesTask, categoriesTask);

        var blogsResult = await blogsTask;
        var nichesResult = await nichesTask;
        var categoriesResult = await categoriesTask;

        if (blogsResult.IsUnauthorized)
            return ApiResult<BlogListVm>.ForceSignOut();

        if (!blogsResult.IsSuccess || blogsResult.Data is null)
            return ApiResult<BlogListVm>.Fail(
                blogsResult.StatusCode, blogsResult.Error ?? "Could not load blogs.");

        var data = blogsResult.Data;

        var niches = nichesResult is { IsSuccess: true, Data: { } n }
            ? n.Where(x => x.IsActive).Select(BlogsMapper.ToNicheVm).ToList()
            : [];

        var categories = categoriesResult is { IsSuccess: true, Data: { } c }
            ? c.Where(x => x.IsActive).Select(BlogsMapper.ToCategoryBadgeVm).ToList()
            : [];

        var vm = new BlogListVm
        {
            Blogs      = data.Items.Select(BlogsMapper.ToCardVm).ToList(),
            Niches     = niches,
            Categories = categories,
            Pager      = new BlogPagerVm
            {
                PageNumber      = data.PageNumber,
                PageSize        = data.PageSize,
                TotalCount      = data.TotalCount,
                TotalPages      = data.TotalPages,
                HasPreviousPage = data.HasPreviousPage,
                HasNextPage     = data.HasNextPage,
                Search          = search,
                IsFeatured      = isFeatured,
            },
        };

        return ApiResult<BlogListVm>.Ok(vm);
    }

    public async Task<ApiResult<BlogDetailsVm>> GetDetailsAsync(string slug, CancellationToken ct = default)
    {
        var blogResult = await _blogs.GetBlogBySlugAsync(slug, ct);

        if (blogResult.IsUnauthorized)
            return ApiResult<BlogDetailsVm>.ForceSignOut();
        if (blogResult.IsNotFound)
            return ApiResult<BlogDetailsVm>.Fail(404, "Blog not found.");
        if (!blogResult.IsSuccess || blogResult.Data is null)
            return ApiResult<BlogDetailsVm>.Fail(
                blogResult.StatusCode, blogResult.Error ?? "Could not load the blog.");

        var blog = blogResult.Data;

        var commentsTask = _blogs.ListCommentsAsync(blog.Id, 1, CommentPageSize, ct);
        var tagsTask = _tags.ListTagsAsync(ct);

        await Task.WhenAll(commentsTask, tagsTask);

        var commentsResult = await commentsTask;
        var tagsResult = await tagsTask;

        var comments = commentsResult is { IsSuccess: true, Data: { } cd }
            ? cd.Items.Select(BlogsMapper.ToCommentVm).ToList()
            : [];

        var popularTags = tagsResult is { IsSuccess: true, Data: { } td }
            ? td.Where(t => t.IsActive).Take(PopularTagCount).Select(BlogsMapper.ToTagVm).ToList()
            : [];

        BlogAuthorVm? author = null;

        var vm = BlogsMapper.ToDetailsVm(blog, author, comments, popularTags);
        return ApiResult<BlogDetailsVm>.Ok(vm);
    }

    public async Task<ApiResult<BlogAuthorVm>> GetAuthorAsync(string authorSlug, CancellationToken ct = default)
    {
        var result = await _blogs.GetCreatorProfileBySlugAsync(authorSlug, ct);

        if (result.IsUnauthorized) return ApiResult<BlogAuthorVm>.ForceSignOut();
        if (result.IsNotFound) return ApiResult<BlogAuthorVm>.Fail(404, "Author not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BlogAuthorVm>.Fail(result.StatusCode, result.Error ?? "Could not load the author.");

        return ApiResult<BlogAuthorVm>.Ok(BlogsMapper.ToAuthorVm(result.Data, _assetResolver));
    }
}
