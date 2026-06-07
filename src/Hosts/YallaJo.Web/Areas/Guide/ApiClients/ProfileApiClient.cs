using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>
/// Talks to the tour-guide profile endpoints (group /api/v1/guides) and the
/// content-core lookup / attachment endpoints used by the "My Profile" page.
/// </summary>
public sealed class ProfileApiClient
{
    private const string GuidesBase = "/api/v1/guides";
    private const string AttachmentsBase = "/api/v1/content-core/attachments";
    private const string GuideEntityType = "TourGuide";
    private const string ImageAttachmentType = "Image";

    private readonly IApiClient _api;

    public ProfileApiClient(IApiClient api) => _api = api;

    // GET /api/v1/guides/me
    public Task<ApiResult<TourGuideProfileResponse>> GetMyProfileAsync(CancellationToken ct = default)
        => _api.GetAsync<TourGuideProfileResponse>($"{GuidesBase}/me", ct);

    // PUT /api/v1/guides/{guideId}
    public Task<ApiResult> UpdateProfileAsync(
        Guid guideId, UpdateTourGuideProfileRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{GuidesBase}/{guideId}", request, ct);

    // POST /api/v1/guides/{guideId}/languages
    public Task<ApiResult> AddLanguageAsync(
        Guid guideId, AddTourGuideLanguageRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{GuidesBase}/{guideId}/languages", request, ct);

    // DELETE /api/v1/guides/{guideId}/languages/{languageId}
    public Task<ApiResult> RemoveLanguageAsync(
        Guid guideId, Guid languageId, CancellationToken ct = default)
        => _api.DeleteAsync($"{GuidesBase}/{guideId}/languages/{languageId}", ct);

    // POST /api/v1/guides/{guideId}/specializations
    public Task<ApiResult> AddSpecializationAsync(
        Guid guideId, AddTourGuideSpecializationRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{GuidesBase}/{guideId}/specializations", request, ct);

    // PUT /api/v1/guides/me/avatar (persists a URL only)
    public Task<ApiResult> UpdateAvatarAsync(string avatarUrl, CancellationToken ct = default)
        => _api.PutAsync($"{GuidesBase}/me/avatar", new UpdateGuideAvatarRequest(avatarUrl), ct);

    // PUT /api/v1/guides/me/cover-image (persists a URL only)
    public Task<ApiResult> UpdateCoverImageAsync(string coverImageUrl, CancellationToken ct = default)
        => _api.PutAsync($"{GuidesBase}/me/cover-image", new UpdateGuideCoverImageRequest(coverImageUrl), ct);

    // POST multipart /api/v1/content-core/attachments (returns persisted URL)
    public Task<ApiResult<UploadAttachmentResponse>> UploadImageAsync(
        Guid guideId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        // The singular attachment endpoint binds EntityType/EntityId/AttachmentType
        // from multipart form fields ([FromForm]) only — not the query string.
        // Match the working Creator/Admin upload pattern.
        var fields = new Dictionary<string, string>
        {
            ["EntityType"]     = GuideEntityType,
            ["EntityId"]       = guideId.ToString(),
            ["AttachmentType"] = ImageAttachmentType,
        };

        return _api.PostFileAsync<UploadAttachmentResponse>(
            AttachmentsBase, fileStream, fileName, contentType,
            formFields: fields, formFieldName: "file", ct: ct);
    }

    // GET /api/v1/content-core/specializations?activeOnly=true
    public Task<ApiResult<List<SpecializationResponse>>> GetSpecializationsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<SpecializationResponse>>(
            "/api/v1/content-core/specializations?activeOnly=true", ct);

    // GET /api/v1/content-core/languages?activeOnly=true
    public Task<ApiResult<List<LanguageResponse>>> GetLanguagesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<LanguageResponse>>(
            "/api/v1/content-core/languages?activeOnly=true", ct);
}
