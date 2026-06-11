using YallaJo.Web.Features.Blogs.ApiClients;
using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Articles;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

/// <summary>
/// Orchestrates the creator articles flow (CCD-4). Registered automatically by
/// <c>AddFeatureServices()</c> (name ends in "Facade").
/// <para>
/// Reuse policy: create / admin-get edit-prefetch / update are delegated to the
/// existing <see cref="BlogsApiClient"/> (correct contracts). List (paged+status),
/// submit-for-review (with RowVersion), delete and restore use the CCD-4
/// <see cref="CreatorArticlesApiClient"/> because the Content client lacks or
/// mis-implements those.
/// </para>
/// </summary>
public sealed class CreatorArticlesFacade
{
    private const int DefaultPageSize = 20;

    private readonly CreatorArticlesApiClient _articles;
    private readonly BlogsApiClient _blogs;

    public CreatorArticlesFacade(CreatorArticlesApiClient articles, BlogsApiClient blogs)
    {
        _articles = articles;
        _blogs = blogs;
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<ApiResult<MyArticlesVm>> GetMyArticlesAsync(
        int page, string? statusFilter, CancellationToken ct = default)
    {
        var normalized = CreatorArticlesMapper.NormalizeStatusFilter(statusFilter);

        // The page list and the per-status counts are independent reads — fan out (API1).
        var listTask = _articles.ListMyArticlesAsync(Math.Max(1, page), DefaultPageSize, normalized, ct);
        var countsTask = GetStatusCountsAsync(ct);
        await Task.WhenAll(listTask, countsTask).ConfigureAwait(false);

        var result = await listTask.ConfigureAwait(false);

        if (result.RequireSignOut) return ApiResult<MyArticlesVm>.ForceSignOut();
        if (result.IsForbidden)
            return ApiResult<MyArticlesVm>.Fail(403, "You don't have permission to view your articles.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<MyArticlesVm>.Fail(result.StatusCode, result.Error ?? "Could not load your articles.");

        var (counts, total) = await countsTask.ConfigureAwait(false);
        return ApiResult<MyArticlesVm>.Ok(
            CreatorArticlesMapper.ToListVm(result.Data, normalized, counts, total));
    }

    /// <summary>
    /// Best-effort per-status counts for the filter tabs. Returns (null, null) on any
    /// failure so the list still renders (tabs simply omit the count badges — ERR3).
    /// </summary>
    private async Task<(IReadOnlyDictionary<string, int>? Counts, int? Total)> GetStatusCountsAsync(
        CancellationToken ct)
    {
        try
        {
            var result = await _articles.GetMyStatusCountsAsync(ct).ConfigureAwait(false);
            if (result is not { IsSuccess: true, Data: { } c })
                return (null, null);

            // Only the active (non-deleted) buckets back the tabs; "Rejected" has no
            // dedicated count in the aggregate and simply renders without a badge.
            var map = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Draft"]         = c.Draft,
                ["PendingReview"] = c.PendingReview,
                ["Published"]     = c.Published,
                ["Archived"]      = c.Archived,
            };
            var total = c.Draft + c.PendingReview + c.Published + c.Archived;
            return (map, total);
        }
        catch
        {
            return (null, null);
        }
    }

    // ── Create ────────────────────────────────────────────────────────────────

    /// <summary>Create a Draft article. Returns the new id on success.</summary>
    public async Task<ApiResult<Guid>> CreateAsync(ArticleEditorVm form, CancellationToken ct = default)
    {
        var result = await _blogs.CreateBlogAsync(CreatorArticlesMapper.ToCreateBody(form), ct).ConfigureAwait(false);

        if (result.RequireSignOut) return ApiResult<Guid>.ForceSignOut();
        if (result is { IsSuccess: true, Data: { } data })
            return ApiResult<Guid>.Ok(data.BlogId, result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult<Guid>.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult<Guid>.Fail(result.StatusCode, result.Error ?? FriendlyError(result.StatusCode, "create"));
    }

    // ── Edit prefetch (admin-get → RowVersion + Status) ───────────────────────

    public async Task<ApiResult<ArticleEditorVm>> GetEditorAsync(Guid id, CancellationToken ct = default)
    {
        // IMPORTANT: admin-get is the edit source because it returns RowVersion + Status.
        // The anonymous GET /api/v1/blogs/{id} is Published-only and has no RowVersion.
        var result = await _blogs.GetAdminBlogAsync(id, ct).ConfigureAwait(false);

        if (result.RequireSignOut) return ApiResult<ArticleEditorVm>.ForceSignOut();
        if (result.IsForbidden)
            return ApiResult<ArticleEditorVm>.Fail(403, "You don't have permission to edit this article.");
        if (result.IsNotFound)
            return ApiResult<ArticleEditorVm>.Fail(404, "Article not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ArticleEditorVm>.Fail(result.StatusCode, result.Error ?? "Could not load the article.");

        return ApiResult<ArticleEditorVm>.Ok(CreatorArticlesMapper.ToEditorVm(result.Data));
    }

    // ── Update / Submit / Delete / Restore ────────────────────────────────────

    public async Task<ApiResult> UpdateAsync(Guid id, ArticleEditorVm form, CancellationToken ct = default)
        => Normalize(
            await _blogs.UpdateBlogAsync(id, CreatorArticlesMapper.ToUpdateBody(form), ct).ConfigureAwait(false),
            "update");

    public async Task<ApiResult> SubmitForReviewAsync(Guid id, string rowVersion, CancellationToken ct = default)
        => Normalize(await _articles.SubmitForReviewAsync(id, rowVersion, ct).ConfigureAwait(false), "submit");

    public async Task<ApiResult> DeleteAsync(Guid id, string rowVersion, CancellationToken ct = default)
        => Normalize(await _articles.DeleteArticleAsync(id, rowVersion, ct).ConfigureAwait(false), "delete");

    public async Task<ApiResult> RestoreAsync(Guid id, string rowVersion, CancellationToken ct = default)
        => Normalize(await _articles.RestoreArticleAsync(id, rowVersion, ct).ConfigureAwait(false), "restore");

    // ── Blog ↔ Tour links (§7.2) ────────────────────────────────────────────────

    public async Task<ApiResult> LinkTourAsync(Guid id, Guid tourId, string rowVersion, CancellationToken ct = default)
        => Normalize(await _articles.LinkTourAsync(id, tourId, rowVersion, ct).ConfigureAwait(false), "link the tour to");

    public async Task<ApiResult> UnlinkTourAsync(Guid id, Guid tourId, string rowVersion, CancellationToken ct = default)
        => Normalize(await _articles.UnlinkTourAsync(id, tourId, rowVersion, ct).ConfigureAwait(false), "unlink the tour from");

    // ── Tour autocomplete + chip-name resolution (F10 fix) ──────────────────────

    /// <summary>
    /// Type-ahead suggestions for the tour-link combobox. Returns an empty list on any
    /// failure (the combobox degrades to "no results" rather than erroring).
    /// </summary>
    public async Task<IReadOnlyList<TourSuggestResponse>> SuggestToursAsync(string? q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return [];

        var result = await _articles.SuggestToursAsync(q, ct).ConfigureAwait(false);
        return result is { IsSuccess: true, Data: { } data } ? data : [];
    }

    /// <summary>
    /// Resolves human-readable names for the already-linked tour chips. Names are looked
    /// up in parallel but the set is small and fixed (the tours linked to one article),
    /// so the bounded <see cref="Task.WhenAll(System.Threading.Tasks.Task[])"/> fan-out is
    /// acceptable here (API1); a dedicated batch endpoint (API7) is unnecessary at this size.
    /// Failures are non-fatal: the chip falls back to a short id (handled in the VM).
    /// </summary>
    public async Task<IReadOnlyList<LinkedTourChipVm>> ResolveTourNamesAsync(
        IReadOnlyList<LinkedTourChipVm> chips, CancellationToken ct = default)
    {
        if (chips.Count == 0)
            return chips;

        var lookups = chips
            .Select(async chip =>
            {
                var result = await _articles.GetTourAsync(chip.TourId, ct).ConfigureAwait(false);
                var name = result is { IsSuccess: true, Data: { } tour } ? tour.Name : null;
                return chip with { Name = name };
            })
            .ToList();

        return await Task.WhenAll(lookups).ConfigureAwait(false);
    }

    /// <summary>
    /// No-JS fallback for the link form: the visible combobox submits the typed tour
    /// <em>name</em> (not a GUID). Resolve it to a tour id via the suggest endpoint,
    /// preferring a case-insensitive exact name match, else the single unambiguous result.
    /// Returns <c>null</c> when nothing matches confidently (caller shows a validation error).
    /// </summary>
    public async Task<Guid?> ResolveTourIdByNameAsync(string? tourQuery, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tourQuery))
            return null;

        var matches = await SuggestToursAsync(tourQuery, ct).ConfigureAwait(false);
        if (matches.Count == 0)
            return null;

        var exact = matches.FirstOrDefault(t =>
            string.Equals(t.Name, tourQuery.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact.Id;

        return matches.Count == 1 ? matches[0].Id : null;
    }

    /// <summary>
    /// Fetches the current RowVersion for an article (via admin-get) so destructive
    /// actions (delete) can be issued from the list where the summary lacks RowVersion.
    /// </summary>
    public async Task<ApiResult<(string RowVersion, string Title)>> GetRowVersionAsync(
        Guid id, CancellationToken ct = default)
    {
        var result = await _blogs.GetAdminBlogAsync(id, ct).ConfigureAwait(false);

        if (result.RequireSignOut) return ApiResult<(string, string)>.ForceSignOut();
        if (result.IsForbidden) return ApiResult<(string, string)>.Fail(403, "You don't have permission.");
        if (result.IsNotFound) return ApiResult<(string, string)>.Fail(404, "Article not found.");
        if (!result.IsSuccess || result.Data is null || string.IsNullOrEmpty(result.Data.RowVersion))
            return ApiResult<(string, string)>.Fail(result.StatusCode, result.Error ?? "Could not load the article.");

        return ApiResult<(string, string)>.Ok((result.Data.RowVersion!, result.Data.Title));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ApiResult Normalize(ApiResult result, string verb)
    {
        if (result.RequireSignOut) return ApiResult.ForceSignOut();
        if (result.IsSuccess) return ApiResult.Ok(result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? FriendlyError(result.StatusCode, verb));
    }

    private static string FriendlyError(int statusCode, string verb) => statusCode switch
    {
        403 => "You don't have permission to perform this action.",
        404 => "We couldn't find that article.",
        409 => "This article was modified elsewhere. Please refresh and try again.",
        422 => "This action isn't allowed for the article's current status.",
        _   => $"Could not {verb} the article. Please try again.",
    };
}
