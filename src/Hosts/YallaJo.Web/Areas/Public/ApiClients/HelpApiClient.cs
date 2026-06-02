using YallaJo.Web.Areas.Public.Models.Help;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class HelpApiClient
{
    private readonly IApiClient _api;

    public HelpApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<FaqPageResponse>> GetFaqsAsync(int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<FaqPageResponse>(
            $"/api/v1/seo/faq?activeOnly=true&page={page}&pageSize={pageSize}", ct);
}
