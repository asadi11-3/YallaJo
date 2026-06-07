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

    public async Task<ApiResult<IReadOnlyList<AvailableGuideResponse>>> GetAvailableGuidesAsync(CancellationToken ct = default)
    {
        var result = await _api.GetAvailableGuidesAsync(page: 1, pageSize: AvailableGuidesPageSize, ct: ct);
        if (result.RequireSignOut)
        {
            return ApiResult<IReadOnlyList<AvailableGuideResponse>>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<IReadOnlyList<AvailableGuideResponse>>.Fail(
                result.StatusCode, result.Error ?? "Could not load available guides.");
        }

        return ApiResult<IReadOnlyList<AvailableGuideResponse>>.Ok(result.Data);
    }
}
