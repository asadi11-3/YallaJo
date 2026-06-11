using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.AgencyRoster;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>
/// Agency-OWNER roster management facade. Aggregates the read endpoints into a
/// single roster view and maps mutating-call failures to friendly messages.
/// Distinct from <see cref="GuideAgencyFacade"/> (guide self-service).
/// </summary>
public sealed class GuideAgencyRosterFacade
{
    private const int AvailableGuidesPageSize = 50;

    private readonly AgencyRosterApiClient _api;
    private readonly ILogger<GuideAgencyRosterFacade> _logger;

    public GuideAgencyRosterFacade(AgencyRosterApiClient api, ILogger<GuideAgencyRosterFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<AgencyRosterVm>> GetRosterAsync(CancellationToken ct = default)
    {
        // UI-PERF-API1: independent reads fan out in parallel.
        var guidesTask = _api.GetGuidesAsync(ct);
        var applicationsTask = _api.GetApplicationsAsync(ct);
        var invitationsTask = _api.GetSentInvitationsAsync(ct);
        var availableTask = _api.GetAvailableGuidesAsync(page: 1, pageSize: AvailableGuidesPageSize, ct: ct);
        await Task.WhenAll(guidesTask, applicationsTask, invitationsTask, availableTask);

        var guidesResult = await guidesTask; // UI-PERF-R1: no .Result
        var applicationsResult = await applicationsTask;
        var invitationsResult = await invitationsTask;
        var availableResult = await availableTask;

        if (guidesResult.RequireSignOut || applicationsResult.RequireSignOut || invitationsResult.RequireSignOut)
        {
            return ApiResult<AgencyRosterVm>.ForceSignOut();
        }

        if (!guidesResult.IsSuccess || guidesResult.Data is null)
        {
            return ApiResult<AgencyRosterVm>.Fail(guidesResult.StatusCode, guidesResult.Error ?? "Could not load your agency roster.");
        }

        var guides = guidesResult.Data
            .OrderByDescending(g => g.JoinedAt)
            .Select(AgencyRosterMapper.ToRowVm)
            .ToList();

        var applications = (applicationsResult.Data ?? [])
            .OrderByDescending(a => a.Status == AgencyApplicationStatus.Pending)
            .ThenByDescending(a => a.CreatedAt)
            .Select(AgencyRosterMapper.ToRowVm)
            .ToList();

        var invitations = (invitationsResult.Data ?? [])
            .OrderByDescending(i => i.Status == RosterInvitationStatus.Pending)
            .ThenByDescending(i => i.CreatedAt)
            .Select(AgencyRosterMapper.ToRowVm)
            .ToList();

        // Available-guides failures degrade gracefully (UI-ERR3): the invite picker is a
        // secondary section of the roster page and must not block the primary tables.
        List<AvailableGuideOptionVm> availableGuides = availableResult is { IsSuccess: true, Data: not null }
            ? availableResult.Data.Select(g => new AvailableGuideOptionVm(g.UserId, g.BusinessName, g.ContactEmail)).ToList()
            : [];
        if (!availableResult.IsSuccess)
        {
            _logger.LogWarning("Available-guides lookup failed for the roster invite form (status {StatusCode}).", availableResult.StatusCode);
        }

        var vm = new AgencyRosterVm
        {
            Guides = guides,
            Applications = applications,
            SentInvitations = invitations,
            PendingApplicationCount = applications.Count(a => a.IsPending),
            PendingInvitationCount = invitations.Count(i => i.Status == RosterInvitationStatus.Pending),
            InviteForm = new InviteGuideFormVm { AvailableGuides = availableGuides },
        };

        return ApiResult<AgencyRosterVm>.Ok(vm);
    }

    public async Task<ApiResult> InviteAsync(InviteGuideFormVm form, CancellationToken ct = default)
    {
        var req = new InviteGuideApiRequest(
            form.GuideUserId,
            NullIfBlank(form.Message),
            form.ProposedCommissionPercentage);

        try
        {
            var result = await _api.InviteGuideAsync(req, ct);
            if (result.IsSuccess || result.RequireSignOut)
            {
                return result;
            }

            return ApiResult.Fail(result.StatusCode, MapInviteError(result));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invite guide {GuideUserId} to agency.", form.GuideUserId);
            return ApiResult.Fail(500, "Unable to send the invitation. Please try again.");
        }
    }

    public async Task<ApiResult> ApproveApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.ApproveApplicationAsync(applicationId, ct);
            return result.IsSuccess || result.RequireSignOut
                ? result
                : ApiResult.Fail(result.StatusCode, MapApplicationActionError(result, "approve"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to approve agency application {ApplicationId}.", applicationId);
            return ApiResult.Fail(500, "Unable to approve the application. Please try again.");
        }
    }

    public async Task<ApiResult> RejectApplicationAsync(Guid applicationId, string reason, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.RejectApplicationAsync(applicationId, new RejectAgencyApplicationApiRequest(reason), ct);
            return result.IsSuccess || result.RequireSignOut
                ? result
                : ApiResult.Fail(result.StatusCode, MapApplicationActionError(result, "reject"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reject agency application {ApplicationId}.", applicationId);
            return ApiResult.Fail(500, "Unable to reject the application. Please try again.");
        }
    }

    public async Task<ApiResult> RemoveGuideAsync(Guid guideUserId, string reason, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.RemoveGuideAsync(guideUserId, new RemoveAgencyGuideApiRequest(reason), ct);
            if (result.IsSuccess || result.RequireSignOut)
            {
                return result;
            }

            var message = result.StatusCode switch
            {
                403 => "You can only remove guides from your own agency.",
                404 => "That guide is not currently on your roster.",
                409 => "The roster changed. Reload and try again.",
                422 => result.Error is { Length: > 0 } e ? e : "A reason is required to remove a guide.",
                _ => result.Error ?? "Unable to remove the guide.",
            };
            return ApiResult.Fail(result.StatusCode, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove guide {GuideUserId} from agency.", guideUserId);
            return ApiResult.Fail(500, "Unable to remove the guide. Please try again.");
        }
    }

    private static string MapApplicationActionError(ApiResult result, string verb) => result.StatusCode switch
    {
        403 => "You can only manage applications to your own agency.",
        404 => "That application could not be found.",
        409 => "That application is no longer pending (it may already be approved or rejected).",
        422 => result.Error is { Length: > 0 } e ? e : $"Unable to {verb} the application.",
        _ => result.Error ?? $"Unable to {verb} the application.",
    };

    private static string MapInviteError(ApiResult result) => result.StatusCode switch
    {
        404 => "That guide could not be found, or is no longer available.",
        409 => "That guide is already affiliated or already has a pending invitation.",
        422 => result.Error is { Length: > 0 } e ? e : "The invitation details are invalid.",
        _ => result.Error ?? "Unable to send the invitation.",
    };

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
