using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.SeoSitemap;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class SeoSitemapFacade
{
    private readonly SeoSitemapApiClient _api;

    public SeoSitemapFacade(SeoSitemapApiClient api) => _api = api;

    public async Task<ApiResult<SeoSitemapVm>> GetIndexAsync(SitemapFilterRequest filter, CancellationToken ct = default)
    {
        if (filter.PageSize < 1 || filter.PageSize > 200)
        {
            filter.PageSize = 50;
        }

        if (filter.Page < 1)
        {
            filter.Page = 1;
        }

        var result = await _api.GetEntriesAsync(filter, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SeoSitemapVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SeoSitemapVm>.Fail(result.StatusCode, result.Error ?? "Could not load sitemap entries.");
        }

        return ApiResult<SeoSitemapVm>.Ok(SeoSitemapMapper.ToVm(result.Data, filter));
    }

    public Task<ApiResult> UpdateAsync(UpdateSitemapEntryFormVm form, CancellationToken ct = default)
    {
        var request = new UpdateSitemapEntryApiRequest(form.Priority, NullIfBlank(form.ChangeFrequency));
        return Normalize(_api.UpdateAsync(form.Id, request, ct), "Could not update the sitemap entry.");
    }

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.DeleteAsync(id, ct), "Could not delete the sitemap entry.");

    public Task<ApiResult> RegenerateAsync(CancellationToken ct = default)
        => Normalize(_api.RegenerateAsync(ct), "Could not regenerate the sitemap.");

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
            return ApiResult.Fail(404, "The sitemap entry was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static async Task<ApiResult> Normalize<T>(Task<ApiResult<T>> call, string fallback)
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
            return ApiResult.Fail(404, "The sitemap entry was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
