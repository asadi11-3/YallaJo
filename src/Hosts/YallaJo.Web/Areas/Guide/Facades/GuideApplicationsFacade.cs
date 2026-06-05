using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Applications;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>Composes the guide Applications page: own application list + apply-to-run mutation.</summary>
public sealed class GuideApplicationsFacade
{
    private readonly ApplicationsApiClient _api;
    private readonly ILogger<GuideApplicationsFacade> _logger;

    public GuideApplicationsFacade(ApplicationsApiClient api, ILogger<GuideApplicationsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<ApplicationsVm>> GetAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _api.GetMyApplicationsAsync(page, pageSize, ct);
        if (result.RequireSignOut)
        {
            return ApiResult<ApplicationsVm>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<ApplicationsVm>.Fail(result.StatusCode, result.Error);
        }

        var data = result.Data;

        var vm = new ApplicationsVm
        {
            Applications = data.Items
                .Select(a => new ApplicationRowVm(
                    a.ApplicationId,
                    a.TourId,
                    string.IsNullOrWhiteSpace(a.TourTitle) ? "Untitled tour" : a.TourTitle,
                    a.Status,
                    a.Message,
                    a.ProposedBasePrice,
                    a.CreatedAt,
                    a.ReviewedAt,
                    a.RejectionReason))
                .ToList(),
            TotalCount = data.TotalCount,
            Page = page,
            PageSize = pageSize,
        };

        return ApiResult<ApplicationsVm>.Ok(vm);
    }

    public async Task<ApiResult> ApplyAsync(ApplyForTourFormVm form, CancellationToken ct = default)
    {
        var request = new ApplyForTourRequest(
            form.Message,
            form.RelevantExperience,
            form.ProposedBasePrice,
            ProposedScheduleJson: null);

        try
        {
            return await _api.ApplyForTourAsync(form.TourId, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to submit tour-run application for tour {TourId}.", form.TourId);
            return ApiResult.Fail(500, "Unable to submit your application. Please try again.");
        }
    }
}
