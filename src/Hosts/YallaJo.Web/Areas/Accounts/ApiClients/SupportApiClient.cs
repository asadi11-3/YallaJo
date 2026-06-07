using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Accounts.Models.Support;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

/// <summary>
/// FE-1C — thin HTTP client for the current user's OWN support tickets
/// (Messaging module, group base /api/v1/support). The backend auto-scopes the
/// list/detail/message/close endpoints to the caller when they lack the admin
/// support-queue permission, so this client never sends admin-only parameters
/// (status/category filters, IsInternal, assign, resolve).
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'ApiClient' suffix.
/// </summary>
public sealed class SupportApiClient
{
    private const string Base = "/api/v1/support";

    private readonly IApiClient _api;

    public SupportApiClient(IApiClient api) => _api = api;

    /// <summary>
    /// Paged list of the caller's own tickets (cursor "load more"). No status/category
    /// filters: the backend ignores them for non-admin callers (GetByUserPagedAsync
    /// takes only userId + cursor + pageSize), so we do not pretend to support them.
    /// </summary>
    public Task<ApiResult<SupportTicketPageResponse>> GetTicketsAsync(
        Guid? cursor = null,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (cursor is { } c && c != Guid.Empty)
        {
            query["cursor"] = c.ToString("D");
        }

        var url = QueryHelpers.AddQueryString($"{Base}/tickets", query);
        return _api.GetAsync<SupportTicketPageResponse>(url, ct);
    }

    public Task<ApiResult<SupportTicketItemResponse>> GetTicketAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<SupportTicketItemResponse>($"{Base}/tickets/{id:D}", ct);

    // POST /api/v1/support/tickets/{id}/messages — reply to own ticket (IsInternal is
    // never sent by users; the endpoint forces it false for non-admin callers anyway).
    public Task<ApiResult> PostMessageAsync(Guid id, string body, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/tickets/{id:D}/messages", new { body }, ct);

    // POST /api/v1/support/tickets/{id}/close — close own ticket. RowVersion carries the
    // optimistic-concurrency token from the detail VM (base64); mismatch => 409.
    public Task<ApiResult> CloseAsync(Guid id, string? rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/tickets/{id:D}/close", new { rowVersion }, ct);
}
