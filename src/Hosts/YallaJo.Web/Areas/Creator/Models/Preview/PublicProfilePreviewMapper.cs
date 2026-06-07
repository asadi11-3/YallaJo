using YallaJo.Web.Features.Blogs.Models;

namespace YallaJo.Web.Areas.Creator.Models.Preview;

public static class PublicProfilePreviewMapper
{
    public static PublicProfilePreviewVm NoProfile() => new() { State = PreviewState.NoProfile };

    public static PublicProfilePreviewVm Unavailable() => new() { State = PreviewState.Unavailable };

    public static PublicProfilePreviewVm Active(
        CreatorProfileResponse profile,
        PaginatedResponse<BlogSummaryResponse>? blogs)
    {
        var rows = (blogs?.Items ?? [])
            .Select(b => new PreviewArticleRowVm
            {
                Slug            = b.Slug,
                Title           = b.Title,
                Summary         = b.Summary,
                PublishedAt     = b.PublishedAt,
                ViewCount       = b.ViewCount,
                ReadTimeMinutes = b.ReadTimeMinutes,
            })
            .ToList();

        return new PublicProfilePreviewVm
        {
            State              = PreviewState.Active,
            DisplayName        = profile.DisplayName,
            Bio                = profile.Bio,
            AvatarUrl          = profile.AvatarUrl,
            TrustTier          = profile.TrustTier,
            Slug               = profile.Slug,
            ArticleCount       = profile.ArticleCount,
            FollowerCount      = profile.FollowerCount,
            TotalViewCount     = profile.TotalViewCount,
            TotalReactionCount = profile.TotalReactionCount,
            TotalCommentCount  = profile.TotalCommentCount,
            Articles           = rows,
            Pager = blogs is null
                ? new PreviewPagerVm()
                : new PreviewPagerVm
                {
                    PageNumber      = blogs.PageNumber,
                    PageSize        = blogs.PageSize,
                    TotalCount      = blogs.TotalCount,
                    TotalPages      = blogs.TotalPages,
                    HasPreviousPage = blogs.HasPreviousPage,
                    HasNextPage     = blogs.HasNextPage,
                },
        };
    }
}
