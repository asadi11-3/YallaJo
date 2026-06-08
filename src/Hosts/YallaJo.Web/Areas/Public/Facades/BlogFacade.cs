using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Blog;
using YallaJo.Web.Areas.Public.Translations;
using YallaJo.Web.Features.Blogs.Helpers;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Seo;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class BlogFacade(BlogApiClient api, SeoApiClient seo, TranslationsApiClient translations, IApiAssetUrlResolver assetResolver)
{
    private const int PageSize = 12;
    private const int CreatorPostsCount = 12;

    public async Task<ApiResult<BlogsGridVm>> GetGridAsync(int page, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        var result = await api.GetBlogsAsync(pageNumber, PageSize, ct);
        if (result is not { IsSuccess: true, Data: { } data })
        {
            return ApiResult<BlogsGridVm>.Fail(result.StatusCode, result.Error ?? "Could not load articles.");
        }

        var posts = data.Items.Select(ToCard).ToList();
        return ApiResult<BlogsGridVm>.Ok(new BlogsGridVm
        {
            Posts = posts,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalCount = data.TotalCount,
            HasPreviousPage = data.HasPreviousPage,
            HasNextPage = data.HasNextPage,
        });
    }

    public async Task<ApiResult<BlogPostVm>> GetPostAsync(string slug, CancellationToken ct = default)
    {
        var result = await api.GetBlogBySlugAsync(slug, ct);
        if (result is not { IsSuccess: true, Data: { } d })
        {
            return ApiResult<BlogPostVm>.Fail(result.StatusCode, result.Error ?? "Article not found.");
        }

        var languageCode = TranslationOverlay.ActiveLanguageCode;
        var blogTranslations = await TranslationOverlay.GetApprovedAsync(translations, "Blog", d.Id, languageCode, ct);

        // SEC3: the resolved (translation-overlaid) blog body is rendered via @Html.Raw in
        // Areas/Public/Views/Blog/Post.cshtml, so it MUST be server-side sanitized here at the
        // mapper boundary. Translations are a separate untrusted-HTML source, so sanitize the
        // FINAL resolved value (after TranslationOverlay.Apply), mirroring BlogsMapper.cs.
        var resolvedContent =
            TranslationOverlay.Apply(blogTranslations, "Content", d.Content, languageCode) ?? d.Content;
        var vm = new BlogPostVm
        {
            Id = d.Id,
            Slug = d.Slug,
            Title = TranslationOverlay.Apply(blogTranslations, "Title", d.Title, languageCode) ?? d.Title,
            Content = ContentHtmlSanitizer.Sanitize(resolvedContent),
            Summary = TranslationOverlay.Apply(blogTranslations, "Summary", d.Summary, languageCode),
            PublishedAt = d.PublishedAt,
            ReadTimeMinutes = d.ReadTimeMinutes,
            ViewCount = d.ViewCount,
        };

        var comments = await GetCommentsAsync(d.Id, ct: ct);
        if (comments is { IsSuccess: true, Data: { } commentList })
        {
            vm.Comments = commentList;
        }

        vm.Seo = TranslationOverlay.ApplySeo(
            await GetSeoAsync(SeoEntityType.Blog, d.Id, d.MetaTitle, d.MetaDescription ?? d.Summary, null, ct),
            blogTranslations,
            languageCode,
            fallbackTitle: vm.Title,
            fallbackDescription: vm.Summary);
        return ApiResult<BlogPostVm>.Ok(vm);
    }

    public async Task RecordViewAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await api.RecordViewAsync(id, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { /* view beacons are best-effort */ }
    }

    public async Task<ApiResult<BlogCommentListVm>> GetCommentsAsync(Guid postId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        var size = pageSize < 1 ? 20 : pageSize;
        var result = await api.GetCommentsAsync(postId, pageNumber, size, ct);
        if (result is not { IsSuccess: true, Data: { } data })
        {
            return ApiResult<BlogCommentListVm>.Fail(result.StatusCode, result.Error ?? "Could not load comments.");
        }

        return ApiResult<BlogCommentListVm>.Ok(BlogCommentMapper.ToVm(data));
    }

    public async Task<ApiResult> CreateCommentAsync(Guid postId, CreateBlogCommentFormVm form, CancellationToken ct = default)
    {
        var result = await api.CreateCommentAsync(postId, BlogCommentMapper.ToRequest(form), ct);
        return result.IsSuccess ? result : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not post your comment.");
    }

    public async Task<ApiResult> EditCommentAsync(Guid commentId, EditBlogCommentFormVm form, CancellationToken ct = default)
    {
        var result = await api.EditCommentAsync(commentId, BlogCommentMapper.ToRequest(form), ct);
        return result.IsSuccess ? result : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not update your comment.");
    }

    public async Task<ApiResult> DeleteCommentAsync(Guid commentId, string? rowVersion, CancellationToken ct = default)
    {
        var result = await api.DeleteCommentAsync(commentId, BlogCommentMapper.ToDeleteRequest(rowVersion), ct);
        return result.IsSuccess ? result : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not delete your comment.");
    }

    public async Task<ApiResult> AddCommentReactionAsync(Guid commentId, CancellationToken ct = default)
    {
        var result = await api.AddCommentReactionAsync(commentId, ct);
        return result.IsSuccess ? result : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not like this comment.");
    }

    public async Task<ApiResult> RemoveCommentReactionAsync(Guid commentId, CancellationToken ct = default)
    {
        var result = await api.RemoveCommentReactionAsync(commentId, ct);
        return result.IsSuccess ? result : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not remove your like.");
    }

    public async Task<ApiResult<FollowStateResponse>> GetFollowStateAsync(Guid profileId, CancellationToken ct = default)
    {
        var result = await api.GetFollowStateAsync(profileId, ct);
        if (result is { IsSuccess: true, Data: { } data })
        {
            return ApiResult<FollowStateResponse>.Ok(data);
        }

        return ApiResult<FollowStateResponse>.Fail(result.StatusCode, result.Error ?? "Could not load follow status.");
    }

    public async Task<ApiResult> FollowCreatorAsync(Guid profileId, CancellationToken ct = default)
    {
        var result = await api.FollowCreatorAsync(profileId, ct);
        return result.IsSuccess ? result : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not follow this creator.");
    }

    public async Task<ApiResult> UnfollowCreatorAsync(Guid profileId, CancellationToken ct = default)
    {
        var result = await api.UnfollowCreatorAsync(profileId, ct);
        return result.IsSuccess ? result : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not unfollow this creator.");
    }

    public async Task<ApiResult<CreatorProfileVm>> GetCreatorAsync(string slug, CancellationToken ct = default)
    {
        var result = await api.GetCreatorBySlugAsync(slug, ct);
        if (result is not { IsSuccess: true, Data: { } d })
        {
            return ApiResult<CreatorProfileVm>.Fail(result.StatusCode, result.Error ?? "Creator not found.");
        }

        var avatar = assetResolver.Resolve(d.AvatarUrl);
        var languageCode = TranslationOverlay.ActiveLanguageCode;
        var creatorTranslations = await TranslationOverlay.GetApprovedAsync(translations, "Creator", d.Id, languageCode, ct);
        var displayName = string.IsNullOrWhiteSpace(d.DisplayName) ? "Creator" : d.DisplayName;
        var vm = new CreatorProfileVm
        {
            Id = d.Id,
            Slug = d.Slug,
            DisplayName = TranslationOverlay.Apply(creatorTranslations, "DisplayName", displayName, languageCode) ?? displayName,
            Bio = TranslationOverlay.Apply(creatorTranslations, "Bio", d.Bio, languageCode),
            AvatarUrl = avatar,
            ArticleCount = d.ArticleCount,
            TotalViewCount = d.TotalViewCount,
            FollowerCount = d.FollowerCount,
        };
        vm.Posts = await SafeCreatorBlogsAsync(d.Slug, ct);
        vm.Seo = TranslationOverlay.ApplySeo(
            await GetSeoAsync(SeoEntityType.Creator, d.Id, null, d.Bio, avatar, ct),
            creatorTranslations,
            languageCode,
            fallbackTitle: vm.DisplayName,
            fallbackDescription: vm.Bio);
        return ApiResult<CreatorProfileVm>.Ok(vm);
    }

    private static BlogCardVm ToCard(BlogSummaryResponse b) => new()
    {
        Id = b.Id,
        Slug = b.Slug,
        Title = b.Title,
        Summary = b.Summary,
        PublishedAt = b.PublishedAt,
        ReadTimeMinutes = b.ReadTimeMinutes,
        ViewCount = b.ViewCount,
        IsFeatured = b.IsFeatured,
    };

    private async Task<IReadOnlyList<BlogCardVm>> SafeCreatorBlogsAsync(string slug, CancellationToken ct)
    {
        try
        {
            var result = await api.GetCreatorBlogsAsync(slug, 1, CreatorPostsCount, ct);
            if (result is { IsSuccess: true, Data: { } data })
            {
                return data.Items.Select(ToCard).ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { /* tolerate - creator posts are a best-effort rail */ }
        return [];
    }

    private async Task<SeoContent> GetSeoAsync(
        SeoEntityType entityType,
        Guid id,
        string? fallbackTitle,
        string? fallbackDescription,
        string? fallbackImage,
        CancellationToken ct)
    {
        var metaTitle = fallbackTitle;
        var metaDescription = fallbackDescription;
        string? canonical = null;
        var ogImage = fallbackImage;
        IReadOnlyList<SeoFaqItem> faqs = [];

        try
        {
            var metaTask = seo.GetMetadataAsync(entityType, id, ct);
            var faqTask = seo.GetFaqAsync(entityType, id, ct);
            await Task.WhenAll(metaTask, faqTask);

            if (metaTask.Result is { IsSuccess: true, Data: { } meta })
            {
                if (!string.IsNullOrWhiteSpace(meta.Title)) metaTitle = meta.Title;
                if (!string.IsNullOrWhiteSpace(meta.Description)) metaDescription = meta.Description;
                canonical = meta.Canonical;
                if (!string.IsNullOrWhiteSpace(meta.OgImage)) ogImage = meta.OgImage;
            }

            if (faqTask.Result is { IsSuccess: true, Data: { } items })
            {
                faqs = items
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new SeoFaqItem { Question = f.Question, Answer = f.Answer })
                    .ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { /* SEO is best-effort - never break the page */ }

        return new SeoContent
        {
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            Canonical = canonical,
            OgImage = ogImage,
            Faqs = faqs,
        };
    }
}
