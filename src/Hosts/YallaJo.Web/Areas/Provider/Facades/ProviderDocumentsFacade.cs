using Microsoft.AspNetCore.Http;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.ProviderDocuments;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class ProviderDocumentsFacade
{
    private readonly ProviderDocumentsApiClient _api;

    public ProviderDocumentsFacade(ProviderDocumentsApiClient api) => _api = api;

    public async Task<ApiResult<ProviderDocumentsIndexVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetDocumentsAsync(ct);
        if (result.IsUnauthorized) return ApiResult<ProviderDocumentsIndexVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ProviderDocumentsIndexVm>.Fail(result.StatusCode, result.Error ?? "Could not load documents.");

        return ApiResult<ProviderDocumentsIndexVm>.Ok(new ProviderDocumentsIndexVm
        {
            Documents = result.Data
                .OrderByDescending(d => d.CreatedAt)
                .Select(ProviderDocumentsMapper.ToRowVm)
                .ToList(),
        });
    }

    public async Task<ApiResult> UploadAsync(UploadProviderDocumentFormVm form, CancellationToken ct = default)
    {
        if (form.File is null || form.File.Length == 0)
            return ApiResult.Fail("Choose a file to upload.");

        await using var stream = form.File.OpenReadStream();
        var result = await _api.UploadAsync(stream, form.File.FileName, form.File.ContentType, form.Type, form.ExpiresAt, ct);
        return Normalize(result, "Could not upload the document.");
    }

    public async Task<ApiResult> ReplaceAsync(Guid id, IFormFile? file, DateOnly? expiresAt, string rowVersion, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return ApiResult.Fail("Choose a replacement file.");

        await using var stream = file.OpenReadStream();
        var result = await _api.ReplaceAsync(id, stream, file.FileName, file.ContentType, expiresAt, rowVersion, ct);
        return Normalize(result, "Could not replace the document.");
    }

    private static ApiResult Normalize<T>(ApiResult<T> result, string fallback)
    {
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsForbidden) return ApiResult.Fail(403, "You don't have permission to manage documents.");
        if (result.IsNotFound) return ApiResult.Fail(404, "Document not found.");
        if (result.IsConflict) return ApiResult.Fail(409, result.Error ?? "The document was modified. Reload and try again.");
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
