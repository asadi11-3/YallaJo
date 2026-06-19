using YallaJo.Web.Areas.Creator.Models.Application;
using YallaJo.Web.Areas.Creator.Models.Audience;
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
    /// list of public-safe follower summaries (Gap 3 Phase A): an opaque ordinal and
    /// the followed-at timestamp only — NO follower user IDs are returned or rendered.
    /// Anonymous endpoint, but the Audience page is gated by Creator.Read.
    /// </summary>
    public Task<ApiResult<List<FollowerSummaryResponse>>> ListFollowersAsync(
        Guid profileId, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<List<FollowerSummaryResponse>>(
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
    /// PUT /api/v1/blogs/creators/profile/mine/avatar — update avatar URL (no-JS /
    /// power-user fallback). Permission: Permission.Creator.Update. 404; 409 concurrency.
    /// </summary>
    public Task<ApiResult> UpdateAvatarAsync(
        UpdateCreatorAvatarRequestBody body, CancellationToken ct = default)
        => _api.PutAsync($"{ProfileMinePath}/avatar", body, ct);

    /// <summary>
    /// POST /api/v1/blogs/creators/profile/mine/avatar/upload (CA-1) — managed avatar
    /// upload owned by ContentBlogs. The file is sent multipart under field "file"; the
    /// backend validates content (magic bytes, JPEG/PNG/WEBP, ≤5 MB), stores it under
    /// /uploads/creators/avatars, sets the profile AvatarUrl, and best-effort deletes the
    /// previous local avatar. No ContentCore attachment row is created. Returns no body.
    /// Permission: Permission.Creator.Update. 401; 404; 422 invalid file.
    /// </summary>
    public Task<ApiResult> UploadAvatarImageAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => _api.PostFileAsync(
            $"{ProfileMinePath}/avatar/upload", fileStream, fileName, contentType,
            formFieldName: "file",
            ct: ct);

    /// <summary>
    /// DELETE /api/v1/blogs/creators/profile/mine/avatar (CA-1) — clears the profile
    /// AvatarUrl and best-effort deletes the previous local avatar file.
    /// Permission: Permission.Creator.Update. 401; 404.
    /// </summary>
    public Task<ApiResult> ClearAvatarAsync(CancellationToken ct = default)
        => _api.DeleteAsync($"{ProfileMinePath}/avatar", ct);

    /// <summary>
    /// DELETE /api/v1/blogs/creators/profile/mine — voluntary self-deactivation
    /// (soft delete, 60-day hard-delete window). Permission: Permission.Creator.Delete.
    /// </summary>
    public Task<ApiResult> SelfDeactivateAsync(CancellationToken ct = default)
        => _api.DeleteAsync(ProfileMinePath, ct);
}
