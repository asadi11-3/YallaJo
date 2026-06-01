using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations;

public sealed class SpecializationsFacade
{
    private readonly SpecializationsApiClient _api;
    private readonly IOutputCacheStore _cache;

    public SpecializationsFacade(SpecializationsApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<SpecializationListVm>> GetSpecializationsAsync(bool activeOnly, CancellationToken ct = default)
    {
        var result = await _api.GetSpecializationsAsync(activeOnly, ct);
        if (result.IsSuccess)
            return ApiResult<SpecializationListVm>.CreateSuccess(new SpecializationListVm
            {
                Specializations = (result.Data ?? []).Select(SpecializationsMapper.ToRowVm).ToList(),
            });
        if (result.IsUnauthorized) return ApiResult<SpecializationListVm>.ForceSignOut();
        return ApiResult<SpecializationListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    // Alias kept for controller call-site compatibility (Edit action uses GetAsync).
    public Task<ApiResult<SpecializationListVm>> GetAsync(bool activeOnly, CancellationToken ct = default)
        => GetSpecializationsAsync(activeOnly, ct);

    public async Task<ApiResult> CreateAsync(CreateSpecializationVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.CreateAsync(SpecializationsMapper.ToCreateRequest(vm), ct), "Could not create specialization.", ct);

    public async Task<ApiResult> UpdateAsync(UpdateSpecializationVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.UpdateAsync(vm.Id, SpecializationsMapper.ToUpdateRequest(vm), ct), "Could not update specialization.", ct);

    // Evicts the shared "lookups" output-cache tag whenever a write succeeds.
    private async Task<ApiResult> NormalizeAsync(ApiResult result, string fallback, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await _cache.EvictByTagAsync("lookups", ct);
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
