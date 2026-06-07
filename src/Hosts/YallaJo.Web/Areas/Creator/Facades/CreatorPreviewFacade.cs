using YallaJo.Web.Features.Blogs.ApiClients;
using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Preview;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

/// <summary>
/// Orchestrates the creator public-profile preview (CCD-6). Registered automatically by
/// <c>AddFeatureServices()</c> (name ends in "Facade").
/// <para>
/// Reads the creator's own slug + state from <c>/profile/mine</c> (Creator-area client),
/// then fetches the public profile and published blogs by slug via the existing
/// Content <see cref="BlogsApiClient"/> public methods. The mapper drops internal fields
/// so the preview is public-safe. Read-only — no writes.
/// </para>
/// </summary>
public sealed class CreatorPreviewFacade
{
    private const int PageSize = 20;

    private readonly CreatorApiClient _creator;
    private readonly BlogsApiClient _blogs;

    public CreatorPreviewFacade(CreatorApiClient creator, BlogsApiClient blogs)
    {
        _creator = creator;
        _blogs = blogs;
    }

    public async Task<ApiResult<PublicProfilePreviewVm>> GetPreviewAsync(int page, CancellationToken ct = default)
    {
        // 1. Resolve the caller's own profile (slug + state). 200-with-null-body (the Web
        //    ApiClient surfaces an empty success body as a 200 failure) means "no profile".
        var mineResult = await _creator.GetMyProfileAsync(ct).ConfigureAwait(false);

        if (mineResult.RequireSignOut)
            return ApiResult<PublicProfilePreviewVm>.ForceSignOut();
        if (mineResult.IsForbidden)
            return ApiResult<PublicProfilePreviewVm>.Fail(403, "You don't have permission to preview your profile.");

        if (mineResult is not { IsSuccess: true, Data: { } mine })
        {
            // 200-empty/404 → no profile yet → controller redirects to Application.
            if (mineResult.StatusCode == 200 || mineResult.IsNotFound)
                return ApiResult<PublicProfilePreviewVm>.Ok(PublicProfilePreviewMapper.NoProfile());

            return ApiResult<PublicProfilePreviewVm>.Fail(
                mineResult.StatusCode, mineResult.Error ?? "Could not load your profile.");
        }

        // 2. Not Active (Suspended/Deactivated) → preview unavailable, no public fetch.
        if (!string.Equals(mine.Status, "Active", StringComparison.OrdinalIgnoreCase))
            return ApiResult<PublicProfilePreviewVm>.Ok(PublicProfilePreviewMapper.Unavailable());

        if (string.IsNullOrWhiteSpace(mine.Slug))
            return ApiResult<PublicProfilePreviewVm>.Ok(PublicProfilePreviewMapper.Unavailable());

        // 3. Active → fetch the public profile + published blogs by slug (in parallel).
        var safePage = Math.Max(1, page);
        var profileTask = _blogs.GetCreatorProfileBySlugAsync(mine.Slug, ct);
        var blogsTask = _blogs.GetCreatorBlogsAsync(mine.Slug, safePage, PageSize, ct);

        await Task.WhenAll(profileTask, blogsTask).ConfigureAwait(false);

        var profileResult = await profileTask.ConfigureAwait(false);
        var blogsResult = await blogsTask.ConfigureAwait(false);

        if (profileResult.RequireSignOut || blogsResult.RequireSignOut)
            return ApiResult<PublicProfilePreviewVm>.ForceSignOut();

        // Public profile 404 (e.g. became inactive between calls) → unavailable, no fake data.
        if (profileResult.IsNotFound || profileResult is not { IsSuccess: true, Data: { } publicProfile })
            return ApiResult<PublicProfilePreviewVm>.Ok(PublicProfilePreviewMapper.Unavailable());

        // The published-blogs list is supplementary; a failure degrades to an empty list.
        var publicBlogs = blogsResult is { IsSuccess: true, Data: { } b } ? b : null;

        return ApiResult<PublicProfilePreviewVm>.Ok(
            PublicProfilePreviewMapper.Active(publicProfile, publicBlogs));
    }
}
