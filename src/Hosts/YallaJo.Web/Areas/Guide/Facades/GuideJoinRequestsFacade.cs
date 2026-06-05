using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.JoinRequests;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>
/// Builds the guide "Join Requests" view model and handles approve/reject actions.
/// </summary>
public sealed class GuideJoinRequestsFacade
{
    private readonly JoinRequestsApiClient _api;
    private readonly ILogger<GuideJoinRequestsFacade> _logger;

    public GuideJoinRequestsFacade(JoinRequestsApiClient api, ILogger<GuideJoinRequestsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<JoinRequestsVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetJoinRequestsAsync(ct);
        if (result.RequireSignOut)
        {
            return ApiResult<JoinRequestsVm>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<JoinRequestsVm>.Fail(result.StatusCode, result.Error);
        }

        var rows = result.Data
            .OrderByDescending(r => r.Status == JoinRequestStatus.Pending)
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => new JoinRequestRowVm(
                r.Id,
                r.TourBookingId,
                r.Status,
                r.ParticipantCount,
                r.Message,
                r.ExpiresAt,
                r.RespondedAt,
                r.ResponseMessage,
                r.CreatedAt))
            .ToList();

        return ApiResult<JoinRequestsVm>.Ok(new JoinRequestsVm { Requests = rows });
    }

    public async Task<ApiResult> ApproveAsync(RespondToJoinRequestFormVm form, CancellationToken ct = default)
    {
        try
        {
            var request = new ApproveJoinRequestRequest(NullIfBlank(form.ResponseMessage));
            return await _api.ApproveAsync(form.RequestId, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to approve join request {RequestId}", form.RequestId);
            return ApiResult.Fail(500, "Unable to approve the join request. Please try again.");
        }
    }

    public async Task<ApiResult> RejectAsync(RespondToJoinRequestFormVm form, CancellationToken ct = default)
    {
        try
        {
            var request = new RejectJoinRequestRequest(NullIfBlank(form.ResponseMessage));
            return await _api.RejectAsync(form.RequestId, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reject join request {RequestId}", form.RequestId);
            return ApiResult.Fail(500, "Unable to reject the join request. Please try again.");
        }
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
