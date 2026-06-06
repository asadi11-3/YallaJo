using YallaJo.Web.Areas.Creator.Models.Application;
using YallaJo.Web.Areas.Creator.Models.Dashboard;
using YallaJo.Web.Areas.Creator.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Creator.ApiClients;

public sealed class CreatorApiClient
{
    private const string ProfileMinePath = "/api/v1/blogs/creators/profile/mine";
    private const string ApplicationMinePath = "/api/v1/blogs/creators/applications/mine";
    private const string ApplicationsPath = "/api/v1/blogs/creators/applications";
    private const string RedeemInvitationPath = "/api/v1/blogs/creators/invitations/redeem";
    private const string ProfilesPath = "/api/v1/blogs/creators/profiles";

    private readonly IApiClient _api;

    public CreatorApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<CreatorProfileMineResponse>> GetMyProfileAsync(CancellationToken ct = default)
        => _api.GetAsync<CreatorProfileMineResponse>(ProfileMinePath, ct);

    public Task<ApiResult<CreatorApplicationMineResponse>> GetMyApplicationAsync(CancellationToken ct = default)
        => _api.GetAsync<CreatorApplicationMineResponse>(ApplicationMinePath, ct);

    /// <summary>
    /// GET /api/v1/blogs/creators/profiles/{profileId}/followers — paged follower
    /// list for the Creator-area Audience page (CCD-7). The backend returns a bare
    /// array of follower user IDs (no DTO, no pagination envelope). The Web layer
    /// uses these only as opaque presence signals — the raw GUIDs are NEVER rendered.
    /// Anonymous endpoint, but the Audience page is gated by Creator.Read.
    /// </summary>
    public Task<ApiResult<List<Guid>>> ListFollowersAsync(
        Guid profileId, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<List<Guid>>(
            $"{ProfilesPath}/{profileId:D}/followers?page={page}&pageSize={pageSize}", ct);


    public Task<ApiResult<CreateApplicationResultResponse>> CreateApplicationAsync(
        CreateCreatorApplicationRequestBody body, CancellationToken ct = default)
        => _api.PostAsync<CreateApplicationResultResponse>(ApplicationsPath, body, ct);

    public Task<ApiResult> UpdateApplicationAsync(
        Guid applicationId, UpdateCreatorApplicationRequestBody body, CancellationToken ct = default)
        => _api.PutAsync($"{ApplicationsPath}/{applicationId:D}", body, ct);

    public Task<ApiResult> SubmitApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => _api.PostAsync($"{ApplicationsPath}/{applicationId:D}/submit", null, ct);

    public Task<ApiResult> RedeemInvitationAsync(
        RedeemInvitationRequestBody body, CancellationToken ct = default)
        => _api.PostAsync(RedeemInvitationPath, body, ct);

    // ── Profile writes (CCD-3) ───────────────────────────────────────────────

    /// <summary>
    /// PUT /api/v1/blogs/creators/profile/mine — update own profile (display name,
    /// bio, avatar URL, slug). Permission: Permission.Creator.Update.
    /// 404 no profile; 409 slug-taken/concurrency; 422 validation.
    /// </summary>
    public Task<ApiResult> UpdateProfileAsync(
        UpdateCreatorProfileRequestBody body, CancellationToken ct = default)
        => _api.PutAsync(ProfileMinePath, body, ct);

    /// <summary>
    /// PUT /api/v1/blogs/creators/profile/mine/avatar — update avatar URL.
    /// Permission: Permission.Creator.Update. 404; 409 concurrency.
    /// </summary>
    public Task<ApiResult> UpdateAvatarAsync(
        UpdateCreatorAvatarRequestBody body, CancellationToken ct = default)
        => _api.PutAsync($"{ProfileMinePath}/avatar", body, ct);

    /// <summary>
    /// DELETE /api/v1/blogs/creators/profile/mine — voluntary self-deactivation
    /// (soft delete, 60-day hard-delete window). Permission: Permission.Creator.Delete.
    /// </summary>
    public Task<ApiResult> SelfDeactivateAsync(CancellationToken ct = default)
        => _api.DeleteAsync(ProfileMinePath, ct);
}
