using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Audience;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

/// <summary>
/// Orchestrates the read-only Creator Audience / Followers page (CCD-7). Registered
/// automatically by <c>AddFeatureServices()</c> (name ends in "Facade").
/// <para>
/// Resolves the caller's own profile (count + state) from <c>/profile/mine</c>, then —
/// only for an Active profile — fetches the current page of follower IDs from the
/// public <c>/followers</c> endpoint. The mapper converts those IDs into anonymous
/// ordinal rows; the raw GUIDs never leave this facade. Read-only — no writes.
/// </para>
/// </summary>
public sealed class CreatorAudienceFacade
{
    private const int PageSize = 20;

    private readonly CreatorApiClient _creator;

    public CreatorAudienceFacade(CreatorApiClient creator) => _creator = creator;

    public async Task<ApiResult<CreatorAudienceVm>> GetAudienceAsync(int page, CancellationToken ct = default)
    {
        // 1. Resolve the caller's own profile (FollowerCount + status). 200-with-null-body
        //    (the Web ApiClient surfaces an empty success body as a 200 failure) and 404
        //    both mean "no profile yet".
        var mineResult = await _creator.GetMyProfileAsync(ct).ConfigureAwait(false);

        if (mineResult.RequireSignOut)
            return ApiResult<CreatorAudienceVm>.ForceSignOut();
        if (mineResult.IsForbidden)
            return ApiResult<CreatorAudienceVm>.Fail(403, "You don't have permission to view your audience.");

        if (mineResult is not { IsSuccess: true, Data: { } mine })
        {
            if (mineResult.StatusCode == 200 || mineResult.IsNotFound)
                return ApiResult<CreatorAudienceVm>.Ok(CreatorAudienceMapper.NoProfile());

            return ApiResult<CreatorAudienceVm>.Fail(
                mineResult.StatusCode, mineResult.Error ?? "Could not load your creator profile.");
        }

        // 2. Not Active (Suspended/Deactivated) → audience unavailable, no follower fetch.
        if (!string.Equals(mine.Status, "Active", StringComparison.OrdinalIgnoreCase))
            return ApiResult<CreatorAudienceVm>.Ok(CreatorAudienceMapper.Unavailable(mine.Status));

        // 3. Active → fetch the current page of follower IDs.
        var safePage = Math.Max(1, page);
        var followersResult = await _creator
            .ListFollowersAsync(mine.Id, safePage, PageSize, ct)
            .ConfigureAwait(false);

        if (followersResult.RequireSignOut)
            return ApiResult<CreatorAudienceVm>.ForceSignOut();

        // The follower list is supplementary — a failure degrades to the authoritative
        // count with an empty list rather than failing the whole page.
        var followerIds = followersResult is { IsSuccess: true, Data: { } ids } ? ids : null;

        return ApiResult<CreatorAudienceVm>.Ok(
            CreatorAudienceMapper.ToVm(mine, followerIds, safePage, PageSize));
    }
}
