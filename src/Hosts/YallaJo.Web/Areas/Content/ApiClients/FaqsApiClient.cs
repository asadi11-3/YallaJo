using YallaJo.Web.Areas.Content.Models.Faqs;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.ApiClients;

public sealed class FaqsApiClient
{
    private readonly IApiClient _api;

    public FaqsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<FaqPageResponse>> ListFaqsAsync(
        string? entityType, bool activeOnly, int page, int pageSize, CancellationToken ct = default)
    {
        var query = $"?page={page}&pageSize={pageSize}&activeOnly={(activeOnly ? "true" : "false")}";
        if (!string.IsNullOrWhiteSpace(entityType))
            query += $"&entityType={Uri.EscapeDataString(entityType)}";

        return _api.GetAsync<FaqPageResponse>($"/api/v1/seo/faq{query}", ct);
    }
}
