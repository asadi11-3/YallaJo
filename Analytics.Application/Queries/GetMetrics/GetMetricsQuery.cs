using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetMetrics;

public sealed record GetMetricsQuery(
    DateTime From,
    DateTime To,
    string? Context = null) : IQuery<MetricsResponse>, ICacheableQuery
{
    public string CacheKey => $"analytics:metrics:{From:yyyyMMdd}:{To:yyyyMMdd}:{Context ?? "all"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["analytics:metrics"];
}

public sealed record MetricsResponse(
    IReadOnlyList<PositionMetric> ByPosition,
    IReadOnlyList<VariantMetric> ByVariant);

public sealed record PositionMetric(int Position, int Impressions, int Clicks, int Bookings, decimal Ctr, decimal ConversionRate);
public sealed record VariantMetric(string Variant, int Impressions, int Clicks, decimal Ctr);
