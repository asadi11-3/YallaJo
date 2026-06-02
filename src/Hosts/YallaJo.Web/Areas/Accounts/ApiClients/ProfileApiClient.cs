using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class ProfileApiClient
{
    private readonly IApiClient _api;
    public ProfileApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ProfileResponse>> GetProfileAsync(CancellationToken ct = default)
        => _api.GetAsync<ProfileResponse>("/api/v1/accounts/profile", ct);

    public Task<ApiResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
        => _api.PutAsync("/api/v1/accounts/profile", request, ct);

    public Task<ApiResult> UpdateAvatarAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        return UpdateAvatarInternalAsync(fileStream, fileName, contentType, ct);

        async Task<ApiResult> UpdateAvatarInternalAsync(
            Stream stream, string name, string type, CancellationToken token)
        {
            var result = await _api.PutFileAsync<object>(
                "/api/v1/accounts/profile/avatar", stream, name, type, formFieldName: "file", token);

            return result.IsSuccess
                ? ApiResult.Ok(result.StatusCode)
                : result.IsValidationError
                    ? ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors!)
                    : ApiResult.Fail(result.StatusCode, result.Error);
        }
    }

    public Task<ApiResult> DeleteAvatarAsync(CancellationToken ct = default)
        => _api.DeleteAsync("/api/v1/accounts/profile/avatar", ct);

    public Task<ApiResult> DeleteProfileAsync(CancellationToken ct = default)
        => _api.DeleteAsync("/api/v1/accounts/profile", ct);
}
