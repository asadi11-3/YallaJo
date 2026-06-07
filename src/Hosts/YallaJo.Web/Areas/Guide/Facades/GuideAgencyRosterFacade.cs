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
        var guidesResult = await _api.GetGuidesAsync(ct);
        if (guidesResult.RequireSignOut)
        {
            return ApiResult<AgencyRosterVm>.ForceSignOut();
        }

        if (!guidesResult.IsSuccess || guidesResult.Data is null)
        {
            return ApiResult<AgencyRosterVm>.Fail(guidesResult.StatusCode, guidesResult.Error ?? "Could not load your agency roster.");
        }

        var applicationsResult = await _api.GetApplicationsAsync(ct);
        if (applicationsResult.RequireSignOut)
        {
            return ApiResult<AgencyRosterVm>.ForceSignOut();
        }

        var invitationsResult = await _api.GetSentInvitationsAsync(ct);
        if (invitationsResult.RequireSignOut)
        {
            return ApiResult<AgencyRosterVm>.ForceSignOut();
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

        var vm = new AgencyRosterVm
        {
            Guides = guides,
            Applications = applications,
            SentInvitations = invitations,
            PendingApplicationCount = applications.Count(a => a.IsPending),
            PendingInvitationCount = invitations.Count(i => i.Status == RosterInvitationStatus.Pending),
        };

        return ApiResult<AgencyRosterVm>.Ok(vm);
    }

    /// <summary>Builds the invite form, populating the available-guides picker.</summary>
    public async Task<ApiResult<InviteGuideFormVm>> GetInviteFormAsync(CancellationToken ct = default)
    {
        var result = await _api.GetAvailableGuidesAsync(page: 1, pageSize: AvailableGuidesPageSize, ct: ct);
        if (result.RequireSignOut)
        {
            return ApiResult<InviteGuideFormVm>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<InviteGuideFormVm>.Fail(result.StatusCode, result.Error ?? "Could not load available guides.");
        }

        var options = result.Data
            .Select(g => new AvailableGuideOptionVm(g.UserId, g.BusinessName, g.ContactEmail))
            .ToList();

        return ApiResult<InviteGuideFormVm>.Ok(new InviteGuideFormVm { AvailableGuides = options });
    }

    /// <summary>Re-populates the available-guides picker on an invite form (e.g. after a validation error).</summary>
    public async Task PopulateAvailableGuidesAsync(InviteGuideFormVm form, CancellationToken ct = default)
    {
        var result = await _api.GetAvailableGuidesAsync(page: 1, pageSize: AvailableGuidesPageSize, ct: ct);
        form.AvailableGuides = result.IsSuccess && result.Data is not null
            ? result.Data.Select(g => new AvailableGuideOptionVm(g.UserId, g.BusinessName, g.ContactEmail)).ToList()
            : [];
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
