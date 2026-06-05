using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.MyBusinesses;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class MyBusinessesFacade
{
    private readonly MyBusinessesApiClient _api;

    public MyBusinessesFacade(MyBusinessesApiClient api) => _api = api;

    public async Task<ApiResult<MyBusinessesVm>> GetIndexAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMineAsync(1, 100, ct);
        if (result.IsUnauthorized)
            return ApiResult<MyBusinessesVm>.ForceSignOut();
        if (result is not { IsSuccess: true, Data: not null })
            return ApiResult<MyBusinessesVm>.Fail(result.StatusCode, result.Error ?? "Could not load your businesses.");

        return ApiResult<MyBusinessesVm>.Ok(MyBusinessesMapper.ToVm(result.Data));
    }

    public async Task<ApiResult<ManageBusinessVm>> GetManageAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);
        if (result.IsUnauthorized)
            return ApiResult<ManageBusinessVm>.ForceSignOut();
        if (result is not { IsSuccess: true, Data: not null })
            return ApiResult<ManageBusinessVm>.Fail(result.StatusCode, result.Error ?? "Could not load the business.");

        return ApiResult<ManageBusinessVm>.Ok(MyBusinessesMapper.ToManageVm(result.Data));
    }

    public async Task<ApiResult> UpdateAsync(EditBusinessFormVm form, CancellationToken ct = default)
    {
        var request = new UpdateBusinessApiRequest(
            Name: form.Name.Trim(),
            PlaceId: form.PlaceId,
            Latitude: form.Latitude,
            Longitude: form.Longitude,
            Description: NullIfBlank(form.Description),
            Address: NullIfBlank(form.Address),
            City: NullIfBlank(form.City),
            Country: NullIfBlank(form.Country),
            Phone: NullIfBlank(form.Phone),
            Email: NullIfBlank(form.Email),
            Website: NullIfBlank(form.Website),
            IsHalal: form.IsHalal,
            HasVegetarianOptions: form.HasVegetarianOptions,
            HasAlcoholFreeArea: form.HasAlcoholFreeArea);

        return await Normalize(_api.UpdateAsync(form.Id, request, ct), "Could not update the business.");
    }

    public Task<ApiResult> ResubmitAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.ResubmitAsync(id, ct), "Could not resubmit the business for review.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsForbidden) return ApiResult.Fail(403, "You do not own this business.");
        if (result.IsNotFound) return ApiResult.Fail(404, "The business was not found.");
        if (result.IsConflict) return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
