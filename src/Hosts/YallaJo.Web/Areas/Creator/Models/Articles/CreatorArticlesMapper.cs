using YallaJo.Web.Features.Blogs.Models;

namespace YallaJo.Web.Areas.Creator.Models.Articles;

/// <summary>
/// Pure mapping for the creator articles flow: my-blogs list → VM, admin-get → editor
/// VM, and editor VM → outbound create/update bodies (reusing the Content request body
/// shapes). Dependency-free for unit testing.
/// </summary>
public static class CreatorArticlesMapper
{
    /// <summary>Valid BlogStatus names a creator may filter their own list by.</summary>
    public static readonly IReadOnlyList<string> StatusFilterOptions =
    [
        "Draft", "PendingReview", "Published", "Rejected", "Archived",
    ];

    /// <summary>
    /// Normalizes a requested status filter: returns the canonical BlogStatus name if it
    /// matches a known option (case-insensitive), otherwise null (= no filter / all).
    /// </summary>
    public static string? NormalizeStatusFilter(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return null;

        return StatusFilterOptions.FirstOrDefault(
            o => string.Equals(o, requested.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static MyArticlesVm ToListVm(
        PaginatedResponse<BlogSummaryResponse> page, string? statusFilter)
    {
        var rows = page.Items
            .Select(b => new MyArticleRowVm
            {
                Id                 = b.Id,
                Slug               = b.Slug,
                Title              = b.Title,
                PublishedAt        = b.PublishedAt,
                ViewCount          = b.ViewCount,
                ReadTimeMinutes    = b.ReadTimeMinutes,
                Status             = b.Status,
                SourceLanguageCode = b.SourceLanguageCode,
            })
            .ToList();

        return new MyArticlesVm
        {
            Articles      = rows,
            StatusFilter  = statusFilter,
            StatusOptions = StatusFilterOptions,
            Pager = new ArticlePagerVm
            {
                PageNumber      = page.PageNumber,
                PageSize        = page.PageSize,
                TotalCount      = page.TotalCount,
                TotalPages      = page.TotalPages,
                HasPreviousPage = page.HasPreviousPage,
                HasNextPage     = page.HasNextPage,
                StatusFilter    = statusFilter,
            },
        };
    }

    /// <summary>Maps the admin-get prefetch (RowVersion + Status) into the editor VM.</summary>
    public static ArticleEditorVm ToEditorVm(AdminBlogDetailResponse b)
        => new()
        {
            Id                 = b.Id,
            RowVersion         = b.RowVersion ?? string.Empty,
            Status             = b.Status,
            Title              = b.Title,
            Slug               = b.Slug,
            Content            = b.Content,
            SourceLanguageCode = string.IsNullOrWhiteSpace(b.LanguageCode) ? "en" : b.LanguageCode,
            Summary            = b.Summary,
            MetaTitle          = b.MetaTitle,
            MetaDescription    = b.MetaDescription,
            // Names are resolved later (controller → facade.ResolveTourNamesAsync); here we
            // only carry the ids + order so the editor can render unlink chips.
            LinkedTours        = b.LinkedTours
                .OrderBy(t => t.SortOrder)
                .Select(t => new LinkedTourChipVm(t.TourId, null, t.SortOrder))
                .ToList(),
        };

    public static CreateBlogRequestBody ToCreateBody(ArticleEditorVm form)
        => new(
            Title:              form.Title.Trim(),
            Content:            form.Content,
            SourceLanguageCode: string.IsNullOrWhiteSpace(form.SourceLanguageCode) ? "en" : form.SourceLanguageCode.Trim(),
            Slug:               Trimmed(form.Slug),
            Summary:            Trimmed(form.Summary),
            MetaTitle:          Trimmed(form.MetaTitle),
            MetaDescription:    Trimmed(form.MetaDescription));

    public static UpdateBlogRequestBody ToUpdateBody(ArticleEditorVm form)
        => new(
            RowVersion:      form.RowVersion,
            Title:           form.Title.Trim(),
            Slug:            string.IsNullOrWhiteSpace(form.Slug) ? form.Title.Trim() : form.Slug.Trim(),
            Content:         form.Content,
            Summary:         Trimmed(form.Summary),
            MetaTitle:       Trimmed(form.MetaTitle),
            MetaDescription: Trimmed(form.MetaDescription),
            PlaceId:         null,
            ReadTimeMinutes: null);

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
