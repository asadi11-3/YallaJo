using YallaJo.Web.Features.Blogs.ApiClients;
using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

public sealed class CreatorDashboardFacade
{
    private const int RecentArticlesCount = 5;

    private readonly CreatorApiClient _creator;
    private readonly BlogsApiClient _blogs;
    private readonly CreatorArticlesApiClient _articles;

    public CreatorDashboardFacade(
        CreatorApiClient creator,
        BlogsApiClient blogs,
        CreatorArticlesApiClient articles)
    {
        _creator = creator;
        _blogs = blogs;
        _articles = articles;
    }

    public async Task<ApiResult<CreatorDashboardVm>> GetDashboardAsync(CancellationToken ct = default)
    {
        var profileTask = _creator.GetMyProfileAsync(ct);
        var applicationTask = _creator.GetMyApplicationAsync(ct);

        await Task.WhenAll(profileTask, applicationTask).ConfigureAwait(false);

        var profileResult = await profileTask.ConfigureAwait(false);
        var applicationResult = await applicationTask.ConfigureAwait(false);

        // A 401 on either read means the cookie/JWT expired → bounce to sign-in.
        if (profileResult.RequireSignOut || applicationResult.RequireSignOut)
            return ApiResult<CreatorDashboardVm>.ForceSignOut();

        if (profileResult.IsForbidden || applicationResult.IsForbidden)
            return ApiResult<CreatorDashboardVm>.Fail(
                403, "You don't have permission to view the creator dashboard.");

        if (!TryReadMine(profileResult, out var profile))
            return ApiResult<CreatorDashboardVm>.Fail(
                profileResult.StatusCode, profileResult.Error ?? "Could not load your creator profile.");

        if (!TryReadMine(applicationResult, out var application))
            return ApiResult<CreatorDashboardVm>.Fail(
                applicationResult.StatusCode, applicationResult.Error ?? "Could not load your creator application.");

        IReadOnlyList<CreatorDashboardArticleVm> recentArticles = [];
        CreatorNeedsAttentionVm? needsAttention = null;
        var isActiveProfile = profile is not null
            && string.Equals(profile.Status, "Active", StringComparison.Ordinal);

        if (isActiveProfile)
        {
            // Recent articles and status counts are independent reads — fan out (API1).
            var recentTask = GetRecentArticlesAsync(ct);
            var countsTask = GetNeedsAttentionAsync(ct);
            await Task.WhenAll(recentTask, countsTask).ConfigureAwait(false);
            recentArticles = await recentTask.ConfigureAwait(false);
            needsAttention = await countsTask.ConfigureAwait(false);
        }

        var vm = CreatorDashboardMapper.ToVm(profile, application, recentArticles, needsAttention);
        return ApiResult<CreatorDashboardVm>.Ok(vm);
    }

    private async Task<CreatorNeedsAttentionVm?> GetNeedsAttentionAsync(CancellationToken ct)
    {
        try
        {
            var result = await _articles.GetMyStatusCountsAsync(ct).ConfigureAwait(false);
            if (result is not { IsSuccess: true, Data: { } counts })
                return null;

            return new CreatorNeedsAttentionVm
            {
                Drafts        = counts.Draft,
                PendingReview = counts.PendingReview,
                Deleted       = counts.Deleted,
            };
        }
        catch
        {
            // Needs-attention is a non-critical enhancement — never break the dashboard
            // when the aggregate read fails (ERR3 graceful degradation).
            return null;
        }
    }

    private async Task<IReadOnlyList<CreatorDashboardArticleVm>> GetRecentArticlesAsync(CancellationToken ct)
    {
        try
        {
            // Paginated my-blogs contract (Gap 5); Status now real (Gap 1). Fetch the
            // first page sized to the recent-articles count.
            var result = await _blogs
                .ListMyBlogsAsync(page: 1, pageSize: RecentArticlesCount, ct: ct)
                .ConfigureAwait(false);
            if (result is not { IsSuccess: true, Data: { } page })
                return [];

            return page.Items
                .OrderByDescending(b => b.PublishedAt ?? DateTime.MinValue)
                .Take(RecentArticlesCount)
                .Select(b => new CreatorDashboardArticleVm
                {
                    Title       = b.Title,
                    Status      = b.Status,
                    PublishedAt = b.PublishedAt,
                    ViewCount   = b.ViewCount,
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool TryReadMine<T>(ApiResult<T> result, out T? value) where T : class
    {
        if (result is { IsSuccess: true, Data: { } data })
        {
            value = data;
            return true;
        }

        if (result.StatusCode == 200 || result.IsNotFound)
        {
            value = null;
            return true;
        }

        value = null;
        return false;
    }
}
