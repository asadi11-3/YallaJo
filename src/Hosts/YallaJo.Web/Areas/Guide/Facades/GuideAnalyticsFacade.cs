using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Analytics;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideAnalyticsFacade
{
    private const string DefaultGranularity = "monthly";
    private const int DefaultMonths = 6;
    private const int DefaultPopularToursLimit = 5;

    private readonly AnalyticsApiClient _api;
    private readonly ILogger<GuideAnalyticsFacade> _logger;

    public GuideAnalyticsFacade(AnalyticsApiClient api, ILogger<GuideAnalyticsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<AnalyticsVm>> GetAsync(CancellationToken ct = default)
    {
        var overviewResult = await _api.GetOverviewAsync(ct);
        if (overviewResult.RequireSignOut)
        {
            return ApiResult<AnalyticsVm>.ForceSignOut();
        }

        if (!overviewResult.IsSuccess || overviewResult.Data is null)
        {
            return ApiResult<AnalyticsVm>.Fail(overviewResult.StatusCode, overviewResult.Error);
        }

        var overview = overviewResult.Data;

        var trendsTask = SafeBookingTrendsAsync(ct);
        var popularTask = SafePopularToursAsync(ct);
        var peakTask = SafePeakDaysAsync(ct);

        await Task.WhenAll(trendsTask, popularTask, peakTask);

        var trends = await trendsTask; // UI-PERF-R1: no .Result
        var popular = await popularTask;
        var peak = await peakTask;

        var vm = new AnalyticsVm
        {
            TotalBookings = overview.TotalBookings,
            UpcomingBookings = overview.UpcomingBookings,
            CompletedBookings = overview.CompletedBookings,
            CancellationRate = overview.CancellationRate,
            AverageRating = overview.AverageRating,
            BookingTrends = trends
                .Select(t => new BookingTrendRowVm(t.Period, t.BookingCount))
                .ToList(),
            PopularTours = popular
                .Select(p => new PopularTourRowVm(p.TourId, p.TourName, p.BookingCount))
                .ToList(),
            PeakDays = peak
                .Select(d => new PeakDayRowVm(d.DayOfWeek, d.BookingCount))
                .ToList(),
        };

        return ApiResult<AnalyticsVm>.Ok(vm);
    }

    private async Task<List<BookingTrendResponse>> SafeBookingTrendsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetBookingTrendsAsync(DefaultGranularity, DefaultMonths, ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load guide booking trends.");
            return [];
        }
    }

    private async Task<List<PopularTourResponse>> SafePopularToursAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPopularToursAsync(DefaultPopularToursLimit, ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load guide popular tours.");
            return [];
        }
    }

    private async Task<List<PeakDayStatResponse>> SafePeakDaysAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPeakDaysAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load guide peak days.");
            return [];
        }
    }
}
