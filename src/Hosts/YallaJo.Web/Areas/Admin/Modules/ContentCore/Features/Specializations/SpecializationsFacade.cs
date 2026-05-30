using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations;

public sealed class SpecializationsFacade
{
    private readonly SpecializationsApiClient _api;
    public SpecializationsFacade(SpecializationsApiClient api) => _api = api;

    public async Task<ApiResult<SpecializationListVm>> GetAsync(bool activeOnly, CancellationToken ct = default)
    {
        var result = await _api.GetSpecializationsAsync(activeOnly, ct);

        if (result.IsSuccess)
        {
            var vm = new SpecializationListVm
            {
                Specializations = (result.Data ?? []).Select(SpecializationsMapper.ToRowVm).ToList(),
                ActiveOnly      = activeOnly,
            };
            return ApiResult<SpecializationListVm>.CreateSuccess(vm);
        }

        if (result.IsUnauthorized) return ApiResult<SpecializationListVm>.ForceSignOut();
        return ApiResult<SpecializationListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> CreateAsync(CreateSpecializationVm vm, CancellationToken ct = default)
        => Normalize(await _api.CreateAsync(SpecializationsMapper.ToCreateRequest(vm), ct), "Could not create specialization.");

    public async Task<ApiResult> UpdateAsync(UpdateSpecializationVm vm, CancellationToken ct = default)
        => Normalize(await _api.UpdateAsync(vm.Id, SpecializationsMapper.ToUpdateRequest(vm), ct), "Could not update specialization.");

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsConflict)        return ApiResult.Fail("A specialization with this name already exists.");
        if (result.IsNotFound)        return ApiResult.Fail("Specialization not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
