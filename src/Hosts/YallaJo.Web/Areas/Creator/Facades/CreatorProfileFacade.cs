using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

/// <summary>
/// Orchestrates the creator profile page (CCD-3): view/update profile, avatar URL,
/// and self-deactivation. Registered automatically by <c>AddFeatureServices()</c>
/// (name ends in "Facade").
/// <para>
/// Avatar is URL-only (no Creator/Profile EntityType in ContentCore, so no file upload).
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
