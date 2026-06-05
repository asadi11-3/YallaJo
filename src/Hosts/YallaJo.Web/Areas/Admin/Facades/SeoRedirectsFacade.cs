using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.SeoRedirects;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class SeoRedirectsFacade
{
    private const int DefaultPageSize = 50;
    private readonly SeoRedirectsApiClient _api;

    public SeoRedirectsFacade(SeoRedirectsApiClient api) => _api = api;

    public async Task<ApiResult<RedirectsVm>> GetIndexAsync(RedirectsFilterRequest filter, CancellationToken ct = default)
    {
        if (filter.PageSize < 1 || filter.PageSize > 200)
        {
            filter.PageSize = DefaultPageSize;
        }

        if (filter.Page < 1)
        {
            filter.Page = 1;
        }

        var result = await _api.GetRedirectsAsync(filter, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<RedirectsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<RedirectsVm>.Fail(result.StatusCode, result.Error ?? "Could not load the redirects.");
        }

        return ApiResult<RedirectsVm>.Ok(RedirectsMapper.ToVm(result.Data, filter));
    }

    public Task<ApiResult> CreateAsync(CreateRedirectFormVm form, CancellationToken ct = default)
    {
        var request = new CreateRedirectApiRequest(form.OldUrl.Trim(), form.NewUrl.Trim(), form.StatusCode);
        return Normalize(_api.CreateAsync(request, ct), "Could not create the redirect.");
    }

    public Task<ApiResult> UpdateAsync(UpdateRedirectFormVm form, CancellationToken ct = default)
    {
        var newUrl = string.IsNullOrWhiteSpace(form.NewUrl) ? null : form.NewUrl.Trim();
        var request = new UpdateRedirectApiRequest(newUrl, form.StatusCode, form.IsActive);
        return Normalize(_api.UpdateAsync(form.Id, request, ct), "Could not update the redirect.");
    }

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.DeleteAsync(id, ct), "Could not delete the redirect.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "The redirect was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This would create a redirect chain or conflict. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
