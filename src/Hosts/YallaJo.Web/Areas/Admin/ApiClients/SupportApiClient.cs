using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Support;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class SupportApiClient
{
    private const string Base = "/api/v1/support";
    private readonly IApiClient _api;

    public SupportApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<SupportTicketPageResponse>> GetTicketsAsync(
        string? status, string? category, Guid? cursor, int pageSize, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (!string.IsNullOrWhiteSpace(status))
        {
            query["status"] = status;
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            query["category"] = category;
        }
        if (cursor is { } c && c != Guid.Empty)
        {
            query["cursor"] = c.ToString("D");
        }

        var url = QueryHelpers.AddQueryString($"{Base}/tickets", query);
        return _api.GetAsync<SupportTicketPageResponse>(url, ct);
    }

    public Task<ApiResult<SupportTicketItemResponse>> GetTicketAsync(Guid id, CancellationToken ct)
        => _api.GetAsync<SupportTicketItemResponse>($"{Base}/tickets/{id:D}", ct);

    public Task<ApiResult> PostMessageAsync(Guid id, string body, bool isInternal, CancellationToken ct)
        => _api.PostAsync($"{Base}/tickets/{id:D}/messages", new { body, isInternal }, ct);

    public Task<ApiResult> CloseAsync(Guid id, string? rowVersion, CancellationToken ct)
        => _api.PostAsync($"{Base}/tickets/{id:D}/close", new { rowVersion }, ct);

    public Task<ApiResult> AssignAsync(Guid id, Guid adminUserId, CancellationToken ct)
        => _api.PostAsync($"{Base}/admin/tickets/{id:D}/assign", new { adminUserId }, ct);

    public Task<ApiResult> ResolveAsync(Guid id, string? notes, string? rowVersion, CancellationToken ct)
        => _api.PostAsync($"{Base}/admin/tickets/{id:D}/resolve", new { notes, rowVersion }, ct);
}
