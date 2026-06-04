using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Outbox;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class OutboxApiClient
{
    private const string Base = "/api/v1/ops/outbox";
    private readonly IApiClient _api;

    public OutboxApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<DeadLetterPageResponse>> GetDeadLettersAsync(string? module, int limit, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(module))
        {
            query["module"] = module;
        }

        var url = QueryHelpers.AddQueryString($"{Base}/dead-letters", query);
        return _api.GetAsync<DeadLetterPageResponse>(url, ct);
    }

    public Task<ApiResult> ReplayAsync(string module, Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/dead-letters/{module}/{id:D}/replay", null, ct);
}
