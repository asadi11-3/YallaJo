using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Agency;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideAgencyFacade
{
    private readonly AgencyApiClient _api;
    private readonly ILogger<GuideAgencyFacade> _logger;

    public GuideAgencyFacade(AgencyApiClient api, ILogger<GuideAgencyFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<AgencyVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMyInvitationsAsync(ct);
        if (result.RequireSignOut)
        {
            return ApiResult<AgencyVm>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<AgencyVm>.Fail(result.StatusCode, result.Error);
        }

        var now = DateTime.UtcNow;
        var rows = result.Data
            .OrderByDescending(i => i.Status == AgencyInvitationStatus.Pending)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new AgencyInvitationRowVm(
                i.Id,
                i.AgencyUserId,
                i.Message,
                i.ProposedCommissionPercentage,
                i.Status,
                i.ExpiresAt,
                i.RespondedAt,
                i.CreatedAt))
            .ToList();

        var pendingCount = rows.Count(r => r.Status == AgencyInvitationStatus.Pending && r.ExpiresAt > now);

        return ApiResult<AgencyVm>.Ok(new AgencyVm { Invitations = rows, PendingCount = pendingCount });
    }

    public async Task<ApiResult> ApplyAsync(ApplyToAgencyFormVm form, CancellationToken ct = default)
    {
        var req = new ApplyToAgencyRequest(NullIfBlank(form.Message));
        try
        {
            return await _api.ApplyToAgencyAsync(form.AgencyUserId, req, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to submit agency application for agency {AgencyUserId}.", form.AgencyUserId);
            return ApiResult.Fail(500, "Unable to submit your agency application. Please try again.");
        }
    }

    public async Task<ApiResult> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _api.AcceptInvitationAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to accept agency invitation {InvitationId}.", id);
            return ApiResult.Fail(500, "Unable to accept the invitation. Please try again.");
        }
    }

    public async Task<ApiResult> DeclineAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _api.DeclineInvitationAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decline agency invitation {InvitationId}.", id);
            return ApiResult.Fail(500, "Unable to decline the invitation. Please try again.");
        }
    }

    public async Task<ApiResult> LeaveAsync(CancellationToken ct = default)
    {
        try
        {
            return await _api.LeaveAgencyAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to leave agency.");
            return ApiResult.Fail(500, "Unable to leave the agency. Please try again.");
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
