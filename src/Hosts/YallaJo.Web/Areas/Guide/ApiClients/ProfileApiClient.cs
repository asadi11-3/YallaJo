using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>
/// Talks to the tour-guide profile endpoints (group /api/v1/guides) and the
/// content-core lookup endpoints used by the "My Profile" page.
/// </summary>
public sealed class ProfileApiClient
{
    private const string GuidesBase = "/api/v1/guides";

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

    // DELETE /api/v1/guides/{guideId}/specializations/{specializationId}
    public Task<ApiResult> RemoveSpecializationAsync(
        Guid guideId, Guid specializationId, CancellationToken ct = default)
        => _api.DeleteAsync($"{GuidesBase}/{guideId}/specializations/{specializationId}", ct);

    // DELETE /api/v1/guides/me (self-deactivate the tour-guide profile)
    public Task<ApiResult> DeactivateAsync(CancellationToken ct = default)
        => _api.DeleteAsync($"{GuidesBase}/me", ct);

    // POST multipart /api/v1/guides/me/avatar/upload (managed avatar upload)
    public Task<ApiResult> UploadAvatarAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => _api.PostFileAsync($"{GuidesBase}/me/avatar/upload", fileStream, fileName, contentType, formFieldName: "file", ct: ct);

    // DELETE /api/v1/guides/me/avatar (managed avatar clear)
    public Task<ApiResult> ClearAvatarAsync(CancellationToken ct = default)
        => _api.DeleteAsync($"{GuidesBase}/me/avatar", ct);

    // GET /api/v1/content-core/specializations?activeOnly=true
    public Task<ApiResult<List<SpecializationResponse>>> GetSpecializationsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<SpecializationResponse>>(
            "/api/v1/content-core/specializations?activeOnly=true", ct);

    // GET /api/v1/content-core/languages?activeOnly=true
    public Task<ApiResult<List<LanguageResponse>>> GetLanguagesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<LanguageResponse>>(
            "/api/v1/content-core/languages?activeOnly=true", ct);
}
