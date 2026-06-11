using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

/// <summary>
/// Orchestrates the creator profile page (CCD-3): view/update profile, avatar (file
/// upload or URL), and self-deactivation. Registered automatically by
/// <c>AddFeatureServices()</c> (name ends in "Facade").
/// <para>
/// Avatar file upload posts the image to the shared ContentCore attachments subsystem
/// with <c>EntityType=Creator</c> (added in B1), then feeds the returned URL into the
/// existing PUT /profile/mine/avatar. A URL-only fallback remains for no-JS clients.
/// </para>
/// </summary>
public sealed class CreatorProfileFacade
{
    private readonly CreatorApiClient _creator;

    public CreatorProfileFacade(CreatorApiClient creator) => _creator = creator;

    /// <summary>Builds the profile page VM (null body = no profile yet).</summary>
    public async Task<ApiResult<CreatorProfileVm>> GetProfileAsync(CancellationToken ct = default)
    {
        var result = await _creator.GetMyProfileAsync(ct).ConfigureAwait(false);

        if (result.RequireSignOut) return ApiResult<CreatorProfileVm>.ForceSignOut();
        if (result.IsForbidden)
            return ApiResult<CreatorProfileVm>.Fail(403, "You don't have permission to view your creator profile.");

        // 200-with-null-body (Web ApiClient surfaces empty success as a 200 failure)
        // and 404 both mean "no profile yet".
        if (result is { IsSuccess: true, Data: { } profile })
            return ApiResult<CreatorProfileVm>.Ok(CreatorProfileMapper.ToVm(profile));

        if (result.StatusCode == 200 || result.IsNotFound)
            return ApiResult<CreatorProfileVm>.Ok(CreatorProfileMapper.ToVm(null));

        return ApiResult<CreatorProfileVm>.Fail(
            result.StatusCode, result.Error ?? "Could not load your creator profile.");
    }

    public async Task<ApiResult> UpdateProfileAsync(CreatorProfileVm form, CancellationToken ct = default)
        => Normalize(
            await _creator.UpdateProfileAsync(CreatorProfileMapper.ToUpdateBody(form), ct).ConfigureAwait(false),
            "update your profile");

    public async Task<ApiResult> UpdateAvatarAsync(UpdateAvatarVm form, CancellationToken ct = default)
        => Normalize(
            await _creator.UpdateAvatarAsync(CreatorProfileMapper.ToAvatarBody(form), ct).ConfigureAwait(false),
            "update your avatar");

    /// <summary>
    /// Uploads an avatar image to the ContentCore attachments subsystem (EntityType=Creator,
    /// EntityId=profileId) and then sets it as the creator's avatar via PUT /profile/mine/avatar.
    /// Client-side validates type and size; the API remains the authority (SEC4 magic-byte).
    /// </summary>
    public async Task<ApiResult> UploadAvatarFileAsync(Guid profileId, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return ApiResult.Fail(400, "Please choose an image to upload.");

        var contentType = file.ContentType?.ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png" or "image/gif" or "image/webp"))
            return ApiResult.Fail(422, "Choose a JPEG, PNG, GIF, or WebP image.");

        if (file.Length > 10 * 1024 * 1024)
            return ApiResult.Fail(422, "The image must be 10 MB or smaller.");

        await using var stream = file.OpenReadStream();
        var upload = await _creator
            .UploadAvatarImageAsync(profileId, stream, file.FileName, contentType, ct)
            .ConfigureAwait(false);

        if (upload.RequireSignOut) return ApiResult.ForceSignOut();
        if (upload.IsForbidden) return ApiResult.Fail(403, FriendlyError(403, "update your avatar"));
        if (!upload.IsSuccess || upload.Data is null)
            return ApiResult.Fail(upload.StatusCode, upload.Error ?? FriendlyError(upload.StatusCode, "update your avatar"));

        // Persist the uploaded image URL as the avatar (reuses the existing avatar endpoint).
        return Normalize(
            await _creator.UpdateAvatarAsync(new UpdateCreatorAvatarRequestBody(upload.Data.Url), ct).ConfigureAwait(false),
            "update your avatar");
    }

    public async Task<ApiResult> SelfDeactivateAsync(CancellationToken ct = default)
        => Normalize(await _creator.SelfDeactivateAsync(ct).ConfigureAwait(false), "deactivate your creator profile");

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
        404 => "You don't have a creator profile yet.",
        409 => "That profile URL slug is already taken, or the profile changed. Please try again.",
        422 => "We couldn't process that request. Please review the details and try again.",
        _   => $"Could not {verb}. Please try again.",
    };
}
