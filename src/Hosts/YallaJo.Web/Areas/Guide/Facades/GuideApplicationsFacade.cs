using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Applications;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>Composes the guide Applications page: own application list + apply-to-run mutation.</summary>
public sealed class GuideApplicationsFacade
{
    private const int OpenToursPageSize = 50;

    private readonly ApplicationsApiClient _api;
    private readonly ILogger<GuideApplicationsFacade> _logger;

    public GuideApplicationsFacade(ApplicationsApiClient api, ILogger<GuideApplicationsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<ApplicationsVm>> GetAsync(int page, int pageSize, CancellationToken ct = default)
    {
        // UI-PERF-API1: fetch applications and open tours in parallel.
        var applicationsTask = _api.GetMyApplicationsAsync(page, pageSize, ct);
        var openToursTask = FetchOpenToursAsync(q: null, ct);
        await Task.WhenAll(applicationsTask, openToursTask);

        var result = await applicationsTask; // UI-PERF-R1: no .Result
        var openToursResult = await openToursTask;
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
            // UI-ERR3: open-tour options are an enhancement — degrade to an empty
            // list (the picker still renders with just the placeholder) on failure.
            OpenTours = openToursResult,
        };

        return ApiResult<ApplicationsVm>.Ok(vm);
    }

    /// <summary>First page of tours open for guide applications, mapped to picker options (F10).</summary>
    public async Task<ApiResult<IReadOnlyList<OpenTourOptionVm>>> GetOpenToursAsync(string? q, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.GetOpenToursAsync(q, page: 1, pageSize: OpenToursPageSize, ct);
            if (result.RequireSignOut)
            {
                return ApiResult<IReadOnlyList<OpenTourOptionVm>>.ForceSignOut();
            }

            if (!result.IsSuccess || result.Data is null)
            {
                return ApiResult<IReadOnlyList<OpenTourOptionVm>>.Fail(result.StatusCode, result.Error);
            }

            var options = result.Data.Items
                .Select(t => new OpenTourOptionVm(
                    t.TourId,
                    t.City is null ? t.Title : $"{t.Title} — {t.City}"))
                .ToList();
            return ApiResult<IReadOnlyList<OpenTourOptionVm>>.Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load open-for-application tours (q='{Q}').", q);
            return ApiResult<IReadOnlyList<OpenTourOptionVm>>.Fail(500, "Unable to load tours open for applications.");
        }
    }

    private async Task<IReadOnlyList<OpenTourOptionVm>> FetchOpenToursAsync(string? q, CancellationToken ct)
    {
        // UI-ERR3: graceful degrade for the page load — never let the picker break the page.
        var result = await GetOpenToursAsync(q, ct);
        return result is { IsSuccess: true, Data: not null } ? result.Data : [];
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
