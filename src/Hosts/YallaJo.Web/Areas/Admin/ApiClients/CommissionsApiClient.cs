using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Commissions;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class CommissionsApiClient(IApiClient api)
{
    private readonly IApiClient _api = api;
    private const string Base = "/api/v1/commissions";

    public Task<ApiResult<IReadOnlyList<CommissionRuleResponse>>> GetRulesAsync(
        string? tier, string? currency, bool includeInactive, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(tier))
        {
            query["tier"] = tier;
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query["currency"] = currency;
        }

        if (includeInactive)
        {
            query["includeInactive"] = "true";
        }

        var url = QueryHelpers.AddQueryString(Base, query);
        return _api.GetAsync<IReadOnlyList<CommissionRuleResponse>>(url, ct);
    }

    public Task<ApiResult> CreateAsync(CreateCommissionRuleRequest request, CancellationToken ct = default)
        => _api.PostAsync(Base, new
        {
            request.Tier,
            request.MinMonthlyRevenue,
            request.MaxMonthlyRevenue,
            request.Currency,
            request.Percentage,
            request.Notes,
        }, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateCommissionRuleRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/{id:D}", new
        {
            request.MinMonthlyRevenue,
            request.MaxMonthlyRevenue,
            request.Percentage,
            request.Notes,
        }, ct);

    public Task<ApiResult> DeleteAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(reason))
        {
            query["reason"] = reason;
        }

        var url = QueryHelpers.AddQueryString($"{Base}/{id:D}", query);
        return _api.DeleteAsync(url, ct);
    }
}
