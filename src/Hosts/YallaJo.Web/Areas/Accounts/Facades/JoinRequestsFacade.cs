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

    public async Task<ApiResult> SubmitAsync(JoinRequestFormVm form, CancellationToken ct = default)
    {
        // GUIDs are guaranteed non-null/non-empty by JoinRequestFormVm validation (the controller
        // only reaches here when ModelState.IsValid). Default to Empty defensively.
        var request = new SubmitJoinRequestApiRequest(
            form.TourBookingId ?? Guid.Empty,
            form.AvailabilitySlotId ?? Guid.Empty,
            form.ParticipantCount,
            string.IsNullOrWhiteSpace(form.Message) ? null : form.Message.Trim());

        var result = await _api.SubmitAsync(request, ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            return ApiResult.Invalid(result.ValidationErrors);
        }
        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not submit your join request.");
    }
}
