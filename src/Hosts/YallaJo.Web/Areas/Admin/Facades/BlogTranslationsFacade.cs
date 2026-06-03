using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.BlogTranslations;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Composes the admin blog-translation screens over the Phase 5A endpoints.
/// Normalizes <see cref="ApiResult"/> values and maps DTOs to view models.
/// A 404 from "get one translation" is treated as a NEW (empty) form, not an error.
/// </summary>
public sealed class BlogTranslationsFacade
{
    private readonly BlogTranslationsApiClient _api;

    public BlogTranslationsFacade(BlogTranslationsApiClient api) => _api = api;

    public async Task<ApiResult<BlogTranslationsIndexVm>> GetIndexAsync(
        Guid blogId, CancellationToken ct = default)
    {
        var result = await _api.GetTranslationsAsync(blogId, ct);

        if (result.IsUnauthorized) return ApiResult<BlogTranslationsIndexVm>.ForceSignOut();
        if (result.IsNotFound) return ApiResult<BlogTranslationsIndexVm>.Fail(404, "Blog not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BlogTranslationsIndexVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load translations.");

        return ApiResult<BlogTranslationsIndexVm>.Ok(
            BlogTranslationsMapper.ToIndexVm(blogId, result.Data));
    }

    public async Task<ApiResult<BlogTranslationEditVm>> GetEditAsync(
        Guid blogId, string languageCode, CancellationToken ct = default)
    {
        var result = await _api.GetTranslationAsync(blogId, languageCode, ct);

        if (result.IsUnauthorized) return ApiResult<BlogTranslationEditVm>.ForceSignOut();

        // 404 = no translation yet for this language → present an empty "new" form.
        if (result.IsNotFound)
            return ApiResult<BlogTranslationEditVm>.Ok(
                BlogTranslationsMapper.ToNewVm(blogId, languageCode));

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BlogTranslationEditVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load the translation.");

        return ApiResult<BlogTranslationEditVm>.Ok(
            BlogTranslationsMapper.ToEditVm(blogId, result.Data));
    }

    public async Task<ApiResult> SaveAsync(
        Guid blogId, string languageCode, BlogTranslationEditVm vm, CancellationToken ct = default)
    {
        var result = await _api.UpsertTranslationAsync(
            blogId, languageCode, BlogTranslationsMapper.ToRequest(vm), ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        if (result.IsNotFound) return ApiResult.Fail(404, "Blog not found.");
        if (result.IsConflict)
            return ApiResult.Fail(409, "The translation was modified by another request. Please retry.");

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not save the translation.");
    }
}
