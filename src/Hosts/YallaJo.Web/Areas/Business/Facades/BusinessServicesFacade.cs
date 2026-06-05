using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Services;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessServicesFacade
{
    private readonly ServicesApiClient _api;
    private readonly MyBusinessesApiClient _businesses;

    public BusinessServicesFacade(ServicesApiClient api, MyBusinessesApiClient businesses)
    {
        _api = api;
        _businesses = businesses;
    }

    public async Task<ApiResult<ServicesVm>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        var business = await _businesses.GetByIdAsync(businessId, ct);
        if (business.IsUnauthorized)
            return ApiResult<ServicesVm>.ForceSignOut();
        if (business is not { IsSuccess: true, Data: not null })
            return ApiResult<ServicesVm>.Fail(business.StatusCode, business.Error ?? "Could not load the business.");

        var services = await _api.GetServicesAsync(businessId, ct: ct);
        if (services.IsUnauthorized)
            return ApiResult<ServicesVm>.ForceSignOut();
        if (services is not { IsSuccess: true, Data: not null })
            return ApiResult<ServicesVm>.Fail(services.StatusCode, services.Error ?? "Could not load the services.");

        var vm = new ServicesVm
        {
            BusinessId = businessId,
            BusinessName = business.Data.Name,
            Status = business.Data.Status,
            Services = ServicesMapper.ToRows(services.Data),
        };
        return ApiResult<ServicesVm>.Ok(vm);
    }

    public Task<ApiResult> AddAsync(Guid businessId, AddServiceFormVm form, CancellationToken ct = default)
    {
        var request = new CreateServiceItemApiRequest(
            form.Name.Trim(),
            form.Price,
            form.DurationMinutes,
            form.MaxCapacity,
            string.IsNullOrWhiteSpace(form.Currency) ? "JOD" : form.Currency.Trim().ToUpperInvariant(),
            form.Category,
            NullIfBlank(form.Description),
            form.SortOrder);
        return Normalize(_api.AddAsync(businessId, request, ct), "Could not add the service.");
    }

    public Task<ApiResult> RemoveAsync(Guid serviceId, CancellationToken ct = default) =>
        Normalize(_api.RemoveAsync(serviceId, ct), "Could not delete the service.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsForbidden) return ApiResult.Fail(403, "You do not own this business.");
        if (result.IsNotFound) return ApiResult.Fail(404, "The business or service was not found.");
        if (result.IsConflict) return ApiResult.Fail(409, "This service conflicts with an existing one.");
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
