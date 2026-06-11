using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.MyBusinesses;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class MyBusinessesFacade
{
    private readonly MyBusinessesApiClient _api;
    private readonly IOutputCacheStore _cache;

    public MyBusinessesFacade(MyBusinessesApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<MyBusinessesVm>> GetIndexAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMineAsync(1, 100, ct);
        if (result.IsUnauthorized)
            return ApiResult<MyBusinessesVm>.ForceSignOut();
        if (result is not { IsSuccess: true, Data: not null })
            return ApiResult<MyBusinessesVm>.Fail(result.StatusCode, result.Error ?? "Could not load your businesses.");

        return ApiResult<MyBusinessesVm>.Ok(MyBusinessesMapper.ToVm(result.Data));
    }

    /// <summary>
    /// Builds the "Register a business" page model: the BusinessType options are
    /// always available; the Place options come from the public places list. A
    /// failure to load places is non-fatal (the view shows an empty-state), so we
    /// only force sign-out on an auth failure.
    /// </summary>
    public async Task<ApiResult<RegisterBusinessVm>> GetRegisterAsync(RegisterBusinessFormVm? form = null, CancellationToken ct = default)
    {
        form ??= new RegisterBusinessFormVm();

        var places = await _api.GetPlaceOptionsAsync(ct: ct);
        if (places.IsUnauthorized)
            return ApiResult<RegisterBusinessVm>.ForceSignOut();

        var placeData = places is { IsSuccess: true, Data: not null } ? places.Data.Items : [];
        var placeItems = MyBusinessesMapper.PlaceOptions(placeData, form.PlaceId == Guid.Empty ? null : form.PlaceId);
        var coords = placeData.ToDictionary(
            p => p.Id.ToString("D"),
            p => new PlaceCoord(p.Latitude, p.Longitude));

        var vm = new RegisterBusinessVm
        {
            Form = form,
            BusinessTypes = MyBusinessesMapper.BusinessTypeOptions(form.BusinessType),
            Places = placeItems,
            PlaceCoordinates = coords,
        };
        return ApiResult<RegisterBusinessVm>.Ok(vm);
    }

    /// <summary>
    /// Creates a business and returns the new id so the controller can redirect
    /// into its management pages.
    /// </summary>
    public async Task<ApiResult<Guid>> RegisterAsync(RegisterBusinessFormVm form, CancellationToken ct = default)
    {
        var request = MyBusinessesMapper.ToCreateRequest(form);
        var result = await _api.CreateAsync(request, ct);

        if (result.IsUnauthorized) return ApiResult<Guid>.ForceSignOut();
        if (result.IsForbidden) return ApiResult<Guid>.Fail(403, "You do not have permission to register a business.");
        if (result.IsConflict) return ApiResult<Guid>.Fail(409, "A business like this already exists. Please reload and try again.");
        if (result.IsValidationError && result.ValidationErrors is { } errors)
            return ApiResult<Guid>.ValidationFail(result.StatusCode, errors);
        if (result is not { IsSuccess: true, Data: not null })
            return ApiResult<Guid>.Fail(result.StatusCode, result.Error ?? "Could not register the business.");

        var newId = result.Data.Id;
        await _cache.EvictByTagAsync($"business:{newId}", ct);
        return ApiResult<Guid>.Ok(newId);
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

        var result = await Normalize(_api.UpdateAsync(form.Id, request, ct), "Could not update the business.");
        if (result.IsSuccess) await _cache.EvictByTagAsync($"business:{form.Id}", ct);
        return result;
    }

    /// <summary>
    /// Typeahead place lookup for the Register form's searchable picker.
    /// Failures are non-fatal for the page; the controller returns an empty list.
    /// </summary>
    public async Task<ApiResult<List<PlaceLookupItemResponse>>> LookupPlacesAsync(string term, CancellationToken ct = default)
    {
        var result = await _api.LookupPlacesAsync(term, 10, ct);
        if (result.IsUnauthorized)
            return ApiResult<List<PlaceLookupItemResponse>>.ForceSignOut();
        return result;
    }

    public async Task<ApiResult> ResubmitAsync(Guid id, CancellationToken ct = default)
    {
        var result = await Normalize(_api.ResubmitAsync(id, ct), "Could not resubmit the business for review.");
        if (result.IsSuccess) await _cache.EvictByTagAsync($"business:{id}", ct);
        return result;
    }

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
