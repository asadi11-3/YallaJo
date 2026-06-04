using System.Globalization;
using YallaJo.Web.Areas.Admin.Models.Home;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class StatisticsApiClient
{
    private readonly IApiClient _api;

    public StatisticsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<InteractionPageResponse>> GetInteractionsAsync(
        string? interactionType,
        string? entityType,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = $"/api/v1/admin/interactions?pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(interactionType))
        {
            query += $"&interactionType={Uri.EscapeDataString(interactionType)}";
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query += $"&entityType={Uri.EscapeDataString(entityType)}";
        }

        return _api.GetAsync<InteractionPageResponse>(query, ct);
    }

    public Task<ApiResult<InteractionPageResponse>> GetUserInteractionsAsync(
        Guid userId,
        int pageSize,
        CancellationToken ct = default)
        => _api.GetAsync<InteractionPageResponse>(
            $"/api/v1/admin/interactions/user/{userId.ToString("D", CultureInfo.InvariantCulture)}?pageSize={pageSize}",
            ct);
}
