using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.JoinRequests;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class JoinRequestsFacade
{
    private readonly JoinRequestsApiClient _api;

    public JoinRequestsFacade(JoinRequestsApiClient api) => _api = api;

    public async Task<ApiResult<MyJoinRequestsVm>> GetMineAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMyJoinRequestsAsync(ct);

        if (result.IsUnauthorized)
            return ApiResult<MyJoinRequestsVm>.ForceSignOut();

        if (result is not { IsSuccess: true, Data: { } data })
            return ApiResult<MyJoinRequestsVm>.Fail(result.StatusCode, result.Error ?? "Could not load your join requests.");

        var rows = data
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new JoinRequestRowVm
            {
                Id = r.Id,
                Status = string.IsNullOrWhiteSpace(r.Status) ? "Pending" : r.Status,
                ParticipantCount = r.ParticipantCount,
                Message = r.Message,
                ExpiresAt = r.ExpiresAt,
                RespondedAt = r.RespondedAt,
                ResponseMessage = r.ResponseMessage,
                ResultingBookingId = r.ResultingBookingId,
                CreatedAt = r.CreatedAt
            })
            .ToList();

        return ApiResult<MyJoinRequestsVm>.Ok(new MyJoinRequestsVm { Requests = rows });
    }
}
