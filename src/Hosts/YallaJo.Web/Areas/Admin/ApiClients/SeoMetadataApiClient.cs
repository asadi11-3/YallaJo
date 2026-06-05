using YallaJo.Web.Areas.Admin.Models.SeoMetadata;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class SeoMetadataApiClient
{
    private const string Base = "/api/v1/seo/metadata";
    private readonly IApiClient _api;

    public SeoMetadataApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<SeoMetadataItemResponse>> GetMetadataAsync(
        SeoEntityType entityType, Guid entityId, CancellationToken ct = default) =>
        _api.GetAsync<SeoMetadataItemResponse>($"{Base}/{entityType}/{entityId:D}", ct);

    public Task<ApiResult<UpsertSeoMetadataResponse>> UpsertAsync(
        UpsertSeoMetadataApiRequest request, CancellationToken ct = default) =>
        _api.PostAsync<UpsertSeoMetadataResponse>(Base, request, ct);

    public Task<ApiResult> UpdateAsync(
        Guid id, UpdateSeoMetadataApiRequest request, CancellationToken ct = default) =>
        _api.PutAsync($"{Base}/{id:D}", request, ct);
}
