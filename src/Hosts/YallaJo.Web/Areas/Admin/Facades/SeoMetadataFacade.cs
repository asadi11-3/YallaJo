using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.SeoMetadata;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class SeoMetadataFacade
{
    private readonly SeoMetadataApiClient _api;

    public SeoMetadataFacade(SeoMetadataApiClient api) => _api = api;

    public async Task<ApiResult<SeoMetadataVm>> GetAsync(
        SeoEntityType entityType, Guid? entityId, CancellationToken ct = default)
    {
        var vm = new SeoMetadataVm
        {
            EntityType = entityType,
            EntityId = entityId,
            Form = new SeoMetadataFormVm
            {
                EntityType = entityType,
                EntityId = entityId ?? Guid.Empty,
                SitemapPriority = 0.5m,
            },
        };

        if (entityId is null || entityId == Guid.Empty)
        {
            return ApiResult<SeoMetadataVm>.Ok(vm);
        }

        var result = await _api.GetMetadataAsync(entityType, entityId.Value, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SeoMetadataVm>.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            vm.HasQueried = true;
            return ApiResult<SeoMetadataVm>.Ok(vm);
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SeoMetadataVm>.Fail(result.StatusCode, result.Error ?? "Could not load the SEO metadata.");
        }

        vm.HasQueried = true;
        vm.Metadata = SeoMetadataMapper.ToDetail(result.Data);
        vm.Form = SeoMetadataMapper.ToForm(result.Data);
        return ApiResult<SeoMetadataVm>.Ok(vm);
    }

    public Task<ApiResult> UpsertAsync(SeoMetadataFormVm form, CancellationToken ct = default)
    {
        var request = new UpsertSeoMetadataApiRequest(
            form.EntityType,
            form.EntityId,
            NullIfBlank(form.MetaTitle),
            NullIfBlank(form.MetaDescription),
            NullIfBlank(form.OgTitle),
            NullIfBlank(form.OgDescription),
            NullIfBlank(form.OgImageUrl),
            NullIfBlank(form.SchemaMarkup),
            form.SitemapPriority,
            NullIfBlank(form.SitemapChangeFrequency),
            NullIfBlank(form.CanonicalUrl));

        return Normalize(_api.UpsertAsync(request, ct), "Could not save the SEO metadata.");
    }

    private static async Task<ApiResult> Normalize<T>(Task<ApiResult<T>> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "The SEO metadata was not found.");
        if (result.IsConflict) return ApiResult.Fail(409, "This change conflicts with the current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
