using YallaJo.Web.Areas.Content.ApiClients;
using YallaJo.Web.Areas.Content.Models.Faqs;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Content.Facades;

public sealed class FaqsFacade
{
    private readonly FaqsApiClient _api;

    public FaqsFacade(FaqsApiClient api) => _api = api;

    public async Task<ApiResult<FaqListVm>> GetListAsync(
        string? entityType, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _api.ListFaqsAsync(entityType, activeOnly: true, page, pageSize, ct);

        if (result.IsUnauthorized)
            return ApiResult<FaqListVm>.ForceSignOut();

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<FaqListVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load FAQs.");

        return ApiResult<FaqListVm>.Ok(FaqsMapper.ToListVm(result.Data));
    }
}
