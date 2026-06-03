using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

/// <summary>
/// Composes the self-service provider-application screens. Normalizes
/// <see cref="ApiResult"/> values and maps API DTOs to view models.
/// A 404 from status means "no application yet" (an empty status VM), not an error.
/// </summary>
public sealed class ProviderFacade
{
    private readonly ProviderApiClient _api;

    public ProviderFacade(ProviderApiClient api) => _api = api;

    public async Task<ApiResult<ProviderStatusVm>> GetStatusAsync(CancellationToken ct = default)
    {
        var result = await _api.GetStatusAsync(ct);

        if (result.IsUnauthorized) return ApiResult<ProviderStatusVm>.ForceSignOut();

        // 404 = the user has not started an application yet → show the Apply CTA.
        if (result.IsNotFound)
            return ApiResult<ProviderStatusVm>.Ok(ProviderMapper.EmptyStatus());

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ProviderStatusVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load your application status.");

        return ApiResult<ProviderStatusVm>.Ok(ProviderMapper.ToStatusVm(result.Data));
    }

    public async Task<ApiResult<RegisterProviderResponse>> RegisterAsync(
        ProviderApplyVm vm, CancellationToken ct = default)
    {
        var result = await _api.RegisterAsync(ProviderMapper.ToRegisterRequest(vm), ct);

        if (result.IsUnauthorized) return ApiResult<RegisterProviderResponse>.ForceSignOut();
        if (result.IsValidationError)
            return ApiResult<RegisterProviderResponse>.ValidationFail(result.StatusCode, result.ValidationErrors!);
        if (result.IsConflict)
            return ApiResult<RegisterProviderResponse>.Fail(409, "You already have a provider application.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<RegisterProviderResponse>.Fail(
                result.StatusCode, result.Error ?? "Could not start your provider application.");

        return ApiResult<RegisterProviderResponse>.Ok(result.Data);
    }

    public async Task<ApiResult> SubmitAsync(CancellationToken ct = default)
    {
        var result = await _api.SubmitAsync(ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "No provider application was found to submit.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not submit your application.");
    }
}
