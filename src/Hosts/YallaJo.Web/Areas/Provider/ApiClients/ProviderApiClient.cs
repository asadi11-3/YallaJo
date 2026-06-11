using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

/// <summary>
/// Typed access to the self-service provider-application endpoints
/// (<c>/api/v1/provider/*</c>). Knows endpoint URLs only; all calls go through
/// <see cref="IApiClient"/> and return <see cref="ApiResult"/>/<see cref="ApiResult{T}"/>.
/// </summary>
public sealed class ProviderApiClient
{
    private readonly IApiClient _api;

    public ProviderApiClient(IApiClient api) => _api = api;

    // GET /api/v1/provider/status
    public Task<ApiResult<ProviderStatusResponse>> GetStatusAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderStatusResponse>("/api/v1/provider/status", ct);

    // GET /api/v1/provider/settings  (business info)
    public Task<ApiResult<ProviderSettingsResponse>> GetSettingsAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderSettingsResponse>("/api/v1/provider/settings", ct);

    // POST /api/v1/provider/register
    public Task<ApiResult<RegisterProviderResponse>> RegisterAsync(
        RegisterProviderRequest request, CancellationToken ct = default)
        => _api.PostAsync<RegisterProviderResponse>("/api/v1/provider/register", request, ct);

    // POST /api/v1/provider/apply  (submit the application for review)
    public Task<ApiResult> SubmitAsync(CancellationToken ct = default)
        => _api.PostAsync("/api/v1/provider/apply", body: null, ct);

    // POST /api/v1/provider/reapply  (reapply after rejection; no body)
    public Task<ApiResult> ReapplyAsync(CancellationToken ct = default)
        => _api.PostAsync("/api/v1/provider/reapply", body: null, ct);

    // POST /api/v1/provider/documents/upload  (multipart: file + documentType + expiresAt?)
    public Task<ApiResult<AddProviderDocumentResponse>> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string documentType,
        DateTime? expiresAt,
        CancellationToken ct = default)
    {
        var fields = new Dictionary<string, string> { ["documentType"] = documentType };
        if (expiresAt.HasValue)
            fields["expiresAt"] = expiresAt.Value.ToString("O");

        return _api.PostFileAsync<AddProviderDocumentResponse>(
            "/api/v1/provider/documents/upload",
            fileStream, fileName, contentType,
            formFields: fields,
            formFieldName: "file",
            ct: ct);
    }

    // PUT /api/v1/provider/documents/{id}  (replace an existing document's stored reference)
    public Task<ApiResult<ReplaceProviderDocumentResponse>> ReplaceDocumentAsync(
        Guid documentId, ReplaceProviderDocumentRequest request, CancellationToken ct = default)
        => _api.PutAsync<ReplaceProviderDocumentResponse>(
            $"/api/v1/provider/documents/{documentId}", request, ct);

    // POST /api/v1/provider/documents/{id}/replace-upload — multipart replacement (real file upload).
    public Task<ApiResult<ReplaceProviderDocumentResponse>> ReplaceDocumentUploadAsync(
        Guid documentId,
        Stream fileStream,
        string fileName,
        string contentType,
        DateTime? expiresAt,
        CancellationToken ct = default)
    {
        var fields = new Dictionary<string, string>();
        if (expiresAt.HasValue)
            fields["expiresAt"] = expiresAt.Value.ToString("O");

        return _api.PostFileAsync<ReplaceProviderDocumentResponse>(
            $"/api/v1/provider/documents/{documentId}/replace-upload",
            fileStream, fileName, contentType, fields, "file", ct);
    }
}
