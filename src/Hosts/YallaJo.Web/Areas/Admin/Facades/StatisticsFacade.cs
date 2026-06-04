using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Statistics;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class StatisticsFacade
{
    private readonly StatisticsApiClient _api;

    public StatisticsFacade(StatisticsApiClient api) => _api = api;

    public async Task<ApiResult<StatisticsVm>> GetAnalyticsAsync(
        StatisticsFilterRequest request,
        CancellationToken ct = default)
    {
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        if (request.UserId is { } userId && userId != Guid.Empty)
        {
            var userResult = await _api.GetUserInteractionsAsync(userId, pageSize, ct);
            if (userResult.IsUnauthorized)
            {
                return ApiResult<StatisticsVm>.ForceSignOut();
            }

            if (userResult is not { IsSuccess: true, Data: not null })
            {
                return ApiResult<StatisticsVm>.Fail(
                    userResult.StatusCode,
                    userResult.Error ?? "Could not load interactions for that user.");
            }

            return ApiResult<StatisticsVm>.Ok(
                StatisticsMapper.ToVm(userResult.Data, userId.ToString(), isUserLookup: true));
        }

        var result = await _api.GetInteractionsAsync(
            request.InteractionType,
            request.EntityType,
            pageSize,
            ct);

        if (result.IsUnauthorized)
        {
            return ApiResult<StatisticsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<StatisticsVm>.Fail(
                result.StatusCode,
                result.Error ?? "Could not load analytics.");
        }

        return ApiResult<StatisticsVm>.Ok(StatisticsMapper.ToVm(result.Data));
    }
}
