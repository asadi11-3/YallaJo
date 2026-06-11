using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

/// <summary>
/// Composes the self-service provider-application screens. Normalizes
/// <see cref="ApiResult"/> values and maps API DTOs to view models.
/// A 404 from status means "no application yet" (an empty status VM), not an error.
/// </summary>
public sealed class ProviderFacade
{
    private readonly ProviderApiClient _api;

    public ProviderFacade(ProviderApiClient api) => _api = api;

    public async Task<ApiResult<ProviderStatusVm>> GetStatusAsync(CancellationToken ct = default)
    {
        var result = await _api.GetStatusAsync(ct);

        if (result.IsUnauthorized) return ApiResult<ProviderStatusVm>.ForceSignOut();

        // 404 = the user has not started an application yet → show the Apply CTA.
        if (result.IsNotFound)
            return ApiResult<ProviderStatusVm>.Ok(ProviderMapper.EmptyStatus());

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ProviderStatusVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load your application status.");

        return ApiResult<ProviderStatusVm>.Ok(ProviderMapper.ToStatusVm(result.Data));
    }

    /// <summary>
    /// Loads the approved provider's business settings
    /// (<c>GET /api/v1/provider/settings</c>). A 404 means "no application yet" —
    /// callers treat any failure as "no business info to show" (non-blocking).
    /// </summary>
    public async Task<ApiResult<ProviderSettingsResponse>> GetSettingsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetSettingsAsync(ct);

        if (result.IsUnauthorized) return ApiResult<ProviderSettingsResponse>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ProviderSettingsResponse>.Fail(
                result.StatusCode, result.Error ?? "Could not load your business settings.");

        return ApiResult<ProviderSettingsResponse>.Ok(result.Data);
    }

    public async Task<ApiResult<RegisterProviderResponse>> RegisterAsync(
        ProviderApplyVm vm, CancellationToken ct = default)
    {
        var result = await _api.RegisterAsync(ProviderMapper.ToRegisterRequest(vm), ct);

        if (result.IsUnauthorized) return ApiResult<RegisterProviderResponse>.ForceSignOut();
        if (result.IsValidationError)
            return ApiResult<RegisterProviderResponse>.ValidationFail(result.StatusCode, result.ValidationErrors!);
        if (result.IsConflict)
            return ApiResult<RegisterProviderResponse>.Fail(409, "You already have a provider application.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<RegisterProviderResponse>.Fail(
                result.StatusCode, result.Error ?? "Could not start your provider application.");

        return ApiResult<RegisterProviderResponse>.Ok(result.Data);
    }

    public async Task<ApiResult> SubmitAsync(CancellationToken ct = default)
    {
        var result = await _api.SubmitAsync(ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "No provider application was found to submit.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not submit your application.");
    }

    public async Task<ApiResult> ReapplyAsync(CancellationToken ct = default)
    {
        var result = await _api.ReapplyAsync(ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "No provider application was found to reapply.");

        // Business-rule failures arrive as 422/400 (invalid status, cooling period still
        // active, or reapplication limit reached). Surface the API message when present.
        if (result.IsValidationError)
            return ApiResult.Fail(
                result.StatusCode,
                result.Error ?? "You cannot reapply right now. Reapply is only available after a rejection once the cooling period has passed.");

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not reapply for your provider application.");
    }

    public async Task<ApiResult<AddProviderDocumentResponse>> UploadDocumentAsync(
        ProviderDocumentUploadVm vm, CancellationToken ct = default)
    {
        // Defensive: the controller's ModelState already enforces these, but guard
        // before touching the stream so the facade never NREs.
        if (vm.File is null || vm.File.Length == 0)
            return ApiResult<AddProviderDocumentResponse>.Fail(400, "Please choose a file to upload.");
        if (string.IsNullOrWhiteSpace(vm.DocumentType))
            return ApiResult<AddProviderDocumentResponse>.Fail(400, "Please choose a document type.");

        await using var stream = vm.File.OpenReadStream();

        var result = await _api.UploadDocumentAsync(
            stream,
            vm.File.FileName,
            vm.File.ContentType,
            vm.DocumentType.Trim(),
            vm.ExpiresAt,
            ct);

        if (result.IsUnauthorized) return ApiResult<AddProviderDocumentResponse>.ForceSignOut();
        if (result.IsValidationError)
            return ApiResult<AddProviderDocumentResponse>.ValidationFail(result.StatusCode, result.ValidationErrors!);
        if (result.IsNotFound)
            return ApiResult<AddProviderDocumentResponse>.Fail(404, "No provider application was found to attach the document to.");
        if (result.IsConflict)
            return ApiResult<AddProviderDocumentResponse>.Fail(409, "A document of this type has already been uploaded.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<AddProviderDocumentResponse>.Fail(
                result.StatusCode, result.Error ?? "Could not upload the document.");

        return ApiResult<AddProviderDocumentResponse>.Ok(result.Data);
    }

    public async Task<ApiResult> ReplaceDocumentAsync(
        ReplaceProviderDocumentVm vm, CancellationToken ct = default)
    {
        // F10: real multipart upload — no manual URL/size typing (UX plan Phase 5).
        if (vm.File is null || vm.File.Length == 0)
            return ApiResult.Fail(400, "Please choose a file to upload.");

        await using var stream = vm.File.OpenReadStream();

        var result = await _api.ReplaceDocumentUploadAsync(
            vm.DocumentId,
            stream,
            vm.File.FileName,
            vm.File.ContentType,
            vm.ExpiresAt,
            ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        if (result.IsNotFound) return ApiResult.Fail(404, "The document to replace was not found.");

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not replace the document.");
    }
}
