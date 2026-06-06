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

    // ---------------------------------------------------------------------
    // Comment + reaction writes (sub-feature A)
    // ---------------------------------------------------------------------

    public Task<ApiResult> PostCommentAsync(
        Guid blogId, string content, Guid? parentCommentId, CancellationToken ct = default)
        => NormalizeAsync(
            () => _blogs.PostCommentAsync(blogId, new CreateBlogCommentRequestBody(content.Trim(), parentCommentId), ct),
            "Could not post your comment.");

    public Task<ApiResult> LikeCommentAsync(Guid commentId, CancellationToken ct = default)
        => NormalizeAsync(
            () => _blogs.AddReactionAsync(commentId, new BlogReactionRequestBody("Like"), ct),
            "Could not record your reaction.");

    public Task<ApiResult> UnlikeCommentAsync(Guid commentId, CancellationToken ct = default)
        => NormalizeAsync(() => _blogs.RemoveReactionAsync(commentId, ct), "Could not remove your reaction.");

    // ---------------------------------------------------------------------
    // Creator profile + follow (sub-feature B)
    // ---------------------------------------------------------------------

    public async Task<ApiResult<CreatorProfileVm>> GetCreatorProfileAsync(
        string slug, CancellationToken ct = default)
    {
        var profileResult = await _blogs.GetCreatorProfileBySlugAsync(slug, ct);

        if (profileResult.IsUnauthorized) return ApiResult<CreatorProfileVm>.ForceSignOut();
        if (profileResult.IsNotFound) return ApiResult<CreatorProfileVm>.Fail(404, "Creator not found.");
        if (!profileResult.IsSuccess || profileResult.Data is null)
            return ApiResult<CreatorProfileVm>.Fail(
                profileResult.StatusCode, profileResult.Error ?? "Could not load the creator profile.");

        var profile = profileResult.Data;

        var blogs = new List<BlogCardVm>();
        try
        {
            var blogsResult = await _blogs.GetCreatorBlogsAsync(slug, 1, 12, ct);
            if (blogsResult is { IsSuccess: true, Data: { } bd })
                blogs = bd.Items.Select(BlogsMapper.ToCardVm).ToList();
        }
        catch
        {
            // tolerate: show the profile even if their blog list fails to load
        }

        var vm = new CreatorProfileVm
        {
            Slug           = profile.Slug,
            DisplayName    = profile.DisplayName,
            Bio            = profile.Bio,
            AvatarUrl      = _assetResolver.Resolve(profile.AvatarUrl) ?? "/assets/images/avatar/01.jpg",
            ArticleCount   = profile.ArticleCount,
            FollowerCount  = profile.FollowerCount,
            TotalViewCount = profile.TotalViewCount,
            FollowTargetId = profile.Id,
            Blogs          = blogs,
        };

        return ApiResult<CreatorProfileVm>.Ok(vm);
    }

    public Task<ApiResult> FollowAsync(Guid profileId, CancellationToken ct = default)
        => NormalizeAsync(() => _blogs.FollowAsync(profileId, ct), "Could not follow this creator.");

    public Task<ApiResult> UnfollowAsync(Guid profileId, CancellationToken ct = default)
        => NormalizeAsync(() => _blogs.UnfollowAsync(profileId, ct), "Could not unfollow this creator.");

    // ---------------------------------------------------------------------
    // Composer: create / edit / my-blogs / submit (sub-feature C)
    // ---------------------------------------------------------------------

    public async Task<ApiResult<CreateBlogResultResponse>> CreateBlogAsync(
        CreateBlogVm form, CancellationToken ct = default)
    {
        var body = new CreateBlogRequestBody(
            form.Title.Trim(),
            form.Content,
            string.IsNullOrWhiteSpace(form.SourceLanguageCode) ? "en" : form.SourceLanguageCode.Trim(),
            string.IsNullOrWhiteSpace(form.Slug) ? null : form.Slug.Trim(),
            string.IsNullOrWhiteSpace(form.Summary) ? null : form.Summary.Trim(),
            string.IsNullOrWhiteSpace(form.MetaTitle) ? null : form.MetaTitle.Trim(),
            string.IsNullOrWhiteSpace(form.MetaDescription) ? null : form.MetaDescription.Trim());

        var result = await _blogs.CreateBlogAsync(body, ct);

        if (result.IsUnauthorized) return ApiResult<CreateBlogResultResponse>.ForceSignOut();
        if (result is { IsSuccess: true, Data: { } d }) return ApiResult<CreateBlogResultResponse>.Ok(d, result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult<CreateBlogResultResponse>.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult<CreateBlogResultResponse>.Fail(result.StatusCode, result.Error ?? "Could not create your article.");
    }

    public async Task<ApiResult<EditBlogVm>> GetEditBlogAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _blogs.GetAdminBlogAsync(id, ct);

        if (result.IsUnauthorized) return ApiResult<EditBlogVm>.ForceSignOut();
        if (result.IsNotFound) return ApiResult<EditBlogVm>.Fail(404, "Article not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<EditBlogVm>.Fail(result.StatusCode, result.Error ?? "Could not load the article.");

        var b = result.Data;
        var vm = new EditBlogVm
        {
            Id                 = b.Id,
            RowVersion         = b.RowVersion ?? string.Empty,
            Title              = b.Title,
            Slug               = b.Slug,
            Content            = b.Content,
            SourceLanguageCode = string.IsNullOrWhiteSpace(b.LanguageCode) ? "en" : b.LanguageCode,
            Summary            = b.Summary,
            MetaTitle          = b.MetaTitle,
            MetaDescription    = b.MetaDescription,
            Status             = b.Status,
        };

        return ApiResult<EditBlogVm>.Ok(vm);
    }

    public Task<ApiResult> UpdateBlogAsync(Guid id, EditBlogVm form, CancellationToken ct = default)
        => NormalizeAsync(
            () => _blogs.UpdateBlogAsync(id, new UpdateBlogRequestBody(
                form.RowVersion,
                form.Title.Trim(),
                string.IsNullOrWhiteSpace(form.Slug) ? form.Title.Trim() : form.Slug.Trim(),
                form.Content,
                string.IsNullOrWhiteSpace(form.Summary) ? null : form.Summary.Trim(),
                string.IsNullOrWhiteSpace(form.MetaTitle) ? null : form.MetaTitle.Trim(),
                string.IsNullOrWhiteSpace(form.MetaDescription) ? null : form.MetaDescription.Trim(),
                null,
                null), ct),
            "Could not update your article.");

    public async Task<ApiResult<MyBlogsVm>> GetMyBlogsAsync(CancellationToken ct = default)
    {
        // Aligned to the paginated my-blogs contract (Gap 5). Status now comes through
        // for real (Gap 1). This legacy Content view shows the first page only.
        var result = await _blogs.ListMyBlogsAsync(ct: ct);

        if (result.IsUnauthorized) return ApiResult<MyBlogsVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<MyBlogsVm>.Fail(result.StatusCode, result.Error ?? "Could not load your articles.");

        var rows = result.Data.Items
            .Select(b => new MyBlogRowVm
            {
                Id          = b.Id,
                Slug        = b.Slug,
                Title       = b.Title,
                Status      = b.Status,
                PublishedAt = b.PublishedAt,
                ViewCount   = b.ViewCount,
            })
            .OrderByDescending(r => r.PublishedAt ?? DateTime.MinValue)
            .ToList();

        return ApiResult<MyBlogsVm>.Ok(new MyBlogsVm { Blogs = rows });
    }

    /// <summary>
    /// Submits a creator's article for review. The backend requires the article's
    /// RowVersion (Gap 5 fix): this controller route only carries the id, so we fetch
    /// the current RowVersion via admin-get, then submit with it.
    /// </summary>
    public async Task<ApiResult> SubmitForReviewAsync(Guid id, CancellationToken ct = default)
    {
        var prefetch = await _blogs.GetAdminBlogAsync(id, ct);
        if (prefetch.IsUnauthorized) return ApiResult.ForceSignOut();
        if (!prefetch.IsSuccess || prefetch.Data is null)
            return ApiResult.Fail(prefetch.StatusCode, prefetch.Error ?? "Could not load your article to submit.");

        var rowVersion = prefetch.Data.RowVersion;
        if (string.IsNullOrEmpty(rowVersion))
            return ApiResult.Fail(409, "Missing version token. Please refresh and try again.");

        return await NormalizeAsync(
            () => _blogs.SubmitForReviewAsync(id, rowVersion, ct),
            "Could not submit your article for review.");
    }

    // ---------------------------------------------------------------------

    private static async Task<ApiResult> NormalizeAsync(Func<Task<ApiResult>> call, string fallback)
    {
        ApiResult result;
        try
        {
            result = await call();
        }
        catch
        {
            return ApiResult.Fail(500, fallback);
        }

        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsSuccess) return ApiResult.Ok(result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
