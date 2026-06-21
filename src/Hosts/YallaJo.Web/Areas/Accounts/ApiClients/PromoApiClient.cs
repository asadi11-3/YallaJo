using YallaJo.Web.Areas.Accounts.Models.Promo;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

/// <summary>
/// Typed proxy over the ContentCore PromoBlocks API endpoints.
/// Auto-registered as scoped by FeatureServiceRegistration (name ends in "ApiClient").
/// </summary>
public sealed class PromoApiClient
{
    private const string BasePath = "/api/v1/content-core/promo-blocks";

    private readonly IApiClient _api;

    public PromoApiClient(IApiClient api) => _api = api;

    /// <summary>Public list: only active + in-window placements.</summary>
    public Task<ApiResult<List<PromoBlockResponse>>> GetPublicAsync(
        IEnumerable<string> keys,
        CancellationToken ct = default)
        => _api.GetAsync<List<PromoBlockResponse>>($"{BasePath}?keys={BuildKeys(keys)}", ct);

    /// <summary>Admin list: includes inactive placements (requires Promotion.Read at the API).</summary>
    public Task<ApiResult<List<PromoBlockResponse>>> GetAdminAsync(
        IEnumerable<string> keys,
        CancellationToken ct = default)
        => _api.GetAsync<List<PromoBlockResponse>>($"{BasePath}/admin?keys={BuildKeys(keys)}", ct);

    /// <summary>Update content fields for a placement (requires Promotion.Update at the API).</summary>
    public Task<ApiResult<PromoBlockEnvelope>> UpdateAsync(
        string key,
        UpdatePromoBlockRequest body,
        CancellationToken ct = default)
        => _api.PutAsync<PromoBlockEnvelope>($"{BasePath}/{Uri.EscapeDataString(key)}", body, ct);

    /// <summary>Upload/replace the placement image (multipart; requires Promotion.Update at the API).</summary>
    public Task<ApiResult<PromoBlockEnvelope>> UploadImageAsync(
        string key,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
        => _api.PostFileAsync<PromoBlockEnvelope>(
            $"{BasePath}/{Uri.EscapeDataString(key)}/image",
            fileStream,
            fileName,
            contentType,
            formFieldName: "file",
            ct: ct);

    private static string BuildKeys(IEnumerable<string> keys)
        => Uri.EscapeDataString(string.Join(',', keys));
}
