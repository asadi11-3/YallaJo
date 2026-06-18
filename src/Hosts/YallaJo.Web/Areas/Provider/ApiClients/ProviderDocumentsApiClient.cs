using System.Globalization;
using YallaJo.Web.Areas.Provider.Models.ProviderDocuments;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

/// <summary>
/// Typed access to the Booking provider-document endpoints
/// (<c>/api/v1/booking/provider/documents</c>). Uploads/replacements are multipart.
/// </summary>
public sealed class ProviderDocumentsApiClient
{
    private const string Base = "/api/v1/booking/provider/documents";

    private readonly IApiClient _api;

    public ProviderDocumentsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/booking/provider/documents
    public Task<ApiResult<List<ProviderDocumentResponse>>> GetDocumentsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<ProviderDocumentResponse>>(Base, ct);

    // POST /api/v1/booking/provider/documents  (multipart: file + type + expiresAt?)
    public Task<ApiResult<UploadProviderDocumentResponse>> UploadAsync(
        Stream fileStream, string fileName, string contentType,
        DocumentType type, DateOnly? expiresAt, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["type"] = ((int)type).ToString(CultureInfo.InvariantCulture),
        };
        if (expiresAt.HasValue)
            fields["expiresAt"] = expiresAt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return _api.PostFileAsync<UploadProviderDocumentResponse>(
            Base, fileStream, fileName, contentType,
            formFields: fields, formFieldName: "file", ct: ct);
    }

    // PUT /api/v1/booking/provider/documents/{id}  (multipart: file + expiresAt? + rowVersion)
    public Task<ApiResult<ProviderDocumentResponse>> ReplaceAsync(
        Guid id, Stream fileStream, string fileName, string contentType,
        DateOnly? expiresAt, string rowVersion, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["rowVersion"] = rowVersion ?? string.Empty,
        };
        if (expiresAt.HasValue)
            fields["expiresAt"] = expiresAt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return _api.PutFileAsync<ProviderDocumentResponse>(
            $"{Base}/{id}", fileStream, fileName, contentType,
            formFields: fields, formFieldName: "file", ct: ct);
    }

    // GET /api/v1/booking/provider/documents/{id}/download — authorized, server-mediated download.
    // The API enforces owner/admin access; the on-disk URL/path is never exposed to the browser.
    public Task<ApiResult<ApiFile>> DownloadAsync(Guid id, CancellationToken ct = default)
        => _api.GetFileAsync($"{Base}/{id}/download", ct);
}
