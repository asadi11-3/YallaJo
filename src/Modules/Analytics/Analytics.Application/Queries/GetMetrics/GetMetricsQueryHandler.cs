using Analytics.Application.Interfaces.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetMetrics;

public sealed class GetMetricsQueryHandler(
    ISuggestionMetricRepository metricRepo) : IQueryHandler<GetMetricsQuery, MetricsResponse>
{
    public async Task<Result<MetricsResponse>> Handle(GetMetricsQuery request, CancellationToken ct)
    {
        var metrics = await metricRepo.GetByDateRangeAsync(request.From, request.To, ct);

        var filtered = request.Context is not null
            ? metrics // Context filtering would need batch→context mapping; simplified for V3
            : metrics;

        // Group by position
        var byPosition = filtered
            .GroupBy(m => m.Position)
            .Select(g =>
            {
                var impressions = g.Count(m => m.Stage == "Impression");
                var clicks = g.Count(m => m.Stage == "Click");
                var bookings = g.Count(m => m.Stage == "Booking");
                var ctr = impressions > 0 ? (decimal)clicks / impressions : 0m;
                var conversion = impressions > 0 ? (decimal)bookings / impressions : 0m;
                return new PositionMetric(g.Key, impressions, clicks, bookings, Math.Round(ctr, 4), Math.Round(conversion, 4));
            })
            .OrderBy(p => p.Position)
            .ToList();

        // Group by experiment variant
        var byVariant = filtered
            .Where(m => m.ExperimentVariant is not null)
            .GroupBy(m => m.ExperimentVariant!)
            .Select(g =>
            {
                var impressions = g.Count(m => m.Stage == "Impression");
                var clicks = g.Count(m => m.Stage == "Click");
                var ctr = impressions > 0 ? (decimal)clicks / impressions : 0m;
                return new VariantMetric(g.Key, impressions, clicks, Math.Round(ctr, 4));
            })
            .OrderBy(v => v.Variant)
            .ToList();

        return Result.Success(new MetricsResponse(byPosition, byVariant));
    }
}
