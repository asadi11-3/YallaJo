# TASK 5 — Provider Analytics

> **Owner:** Mohammad / Mahmoud split — **Hours:** 14h — **Hard deadline:** Sun **2027-02-21 17:00**
> **Earliest start:** Wed 2027-01-20
> **Endpoints:** 3

---

## 1. Endpoint list

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/provider/dashboard` | `MustHavePermission(ProviderDashboard, Read)` + self via ICurrentUser |
| 2 | GET | `/api/v1/provider/analytics?from=&to=&granularity=` | same |
| 3 | GET | `/api/v1/provider/my-tours` | same |

---

## 2. Endpoint #1 — GET /provider/dashboard

Provider's "home" view. Self-only via `BookingProviderSnapshot.UserId == ICurrentUser.UserId`.

```csharp
public sealed record ProviderDashboardDto(
    Guid ProviderId,
    string ProviderName,
    ProviderRevenueSummary Revenue,
    ProviderBookingsSummary Bookings,
    ProviderToursSummary Tours,
    ProviderRatingSummary Rating,
    IReadOnlyList<UpcomingBookingDto> UpcomingBookings,    // next 7 days
    IReadOnlyList<RecentReviewDto> RecentReviews           // last 5
);

public sealed record ProviderRevenueSummary(
    decimal GrossThisMonth,
    decimal NetThisMonth,       // after commission
    decimal PendingPayoutTotal,
    string PrimaryCurrency,
    DateTime? NextPayoutScheduledAt
);

public sealed record ProviderBookingsSummary(
    int PendingConfirmation,     // requires action
    int ConfirmedThisWeek,
    int CompletedThisMonth,
    int CancelledThisMonth
);

public sealed record ProviderToursSummary(
    int Total,
    int Active,
    int Pending,                 // approval pending
    int MostBookedTourId,
    string MostBookedTourName
);

public sealed record ProviderRatingSummary(
    decimal AverageRating,
    int ReviewCount,
    int? RankInCategory          // category percentile from PopularityScore
);

public sealed record UpcomingBookingDto(Guid BookingId, string BookingReference, string TourName, DateTime ScheduledAt, int Participants, decimal TotalAmount);
public sealed record RecentReviewDto(Guid ReviewId, decimal Rating, string? Title, string Content, string ReviewerName, DateTime PublishedAt);
```

**Caching:** `provider:dashboard:{providerId}` 60s sliding. Tags: `provider:dashboard:{providerId}`, `provider-revenue:{providerId}`, `provider-bookings:{providerId}`.

Handler:
```csharp
internal sealed class GetProviderDashboardQueryHandler(
    IBookingProviderSnapshotRepository providerRepo,
    IPaymentSnapshotRepository paymentRepo,
    IBookingSnapshotRepository bookingRepo,
    ITourSnapshotRepository tourRepo,
    IReviewSnapshotRepository reviewRepo,
    IPopularityScoreRepository popRepo,
    ICurrentUser currentUser,
    HybridCache cache,
    TimeProvider time
) : IRequestHandler<GetProviderDashboardQuery, Result<ProviderDashboardDto>>
{
    public async Task<Result<ProviderDashboardDto>> Handle(GetProviderDashboardQuery query, CancellationToken ct)
    {
        var provider = await providerRepo.GetByUserIdAsync(currentUser.UserId!.Value, ct);
        if (provider is null) return Result.Failure<ProviderDashboardDto>(
            new Error("ProviderDashboard.NotProvider", "Current user is not a registered provider"), Outcome.Forbidden);

        var key = $"provider:dashboard:{provider.ProviderId}";
        return await cache.GetOrCreateAsync(
            key,
            async _ => await BuildDashboardAsync(provider, ct),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(60) },
            tags: new[] { key, $"provider-revenue:{provider.ProviderId}" },
            cancellationToken: ct);
    }
}
```

---

## 3. Endpoint #2 — GET /provider/analytics

Provider's time-series dashboard. Scoped to their data only.

```csharp
public sealed record ProviderAnalyticsDto(
    DashboardPeriod Period,
    IReadOnlyList<ProviderRevenueTimePointDto> RevenueSeries,
    IReadOnlyList<ProviderBookingsTimePointDto> BookingsSeries,
    IReadOnlyList<TourPerformanceDto> TourPerformance,
    ConversionFunnelDto Funnel
);

public sealed record ProviderRevenueTimePointDto(DateTime Bucket, decimal Gross, decimal Commission, decimal Refunded, decimal Net);
public sealed record ProviderBookingsTimePointDto(DateTime Bucket, int Created, int Confirmed, int Cancelled, int Completed);

public sealed record TourPerformanceDto(
    Guid TourId,
    string TourName,
    int BookingCount,
    decimal Revenue,
    decimal AverageRating,
    int ReviewCount,
    decimal ConversionRate         // bookings / views from UserInteractions
);

public sealed record ConversionFunnelDto(
    long TotalViews,                // UserInteractions InteractionType=View where EntityId in provider tours
    long Clicks,
    long BookingsStarted,
    long BookingsCompleted,
    decimal ViewToBookingRate
);
```

Filter checks: from/to span valid, granularity supported, provider owns the tours queried.

---

## 4. Endpoint #3 — GET /provider/my-tours

Lightweight list of provider's tours with current stats (for the provider's "My Tours" page in the dashboard).

```csharp
public sealed record ProviderTourListItemDto(
    Guid TourId,
    string TourName,
    string Status,            // Active, Pending, Rejected, Archived
    int BookingCount30d,
    decimal Revenue30d,
    decimal AverageRating,
    int ReviewCount,
    long ViewCount30d
);
```

Cursor pagination same shape.

Cache: `provider:my-tours:{providerId}:cursor:{cursorHash}` 30s sliding.

---

## 5. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | BookingProviderSnapshot / TourSnapshot / ReviewSnapshot read repos (mostly already exist from prior sprints) | 1 | 2027-02-15 |
| 2 | GET /provider/dashboard query handler + DTO | 4 | 2027-02-17 |
| 3 | GET /provider/analytics handler + time-series SQL | 5 | 2027-02-19 |
| 4 | GET /provider/my-tours handler + cursor | 2 | 2027-02-20 |
| 5 | Tests (8+) | 2 | 2027-02-21 |
| **Total** | | **14h** | **Sun 2027-02-21** |

---

## 6. Acceptance tests (8+)

1. GET /provider/dashboard as non-provider → 403 NotProvider.
2. GET /provider/dashboard as provider → 200, ProviderId stamped, totals reasonable.
3. GET /provider/analytics from > to → 422 PeriodInvalid.
4. GET /provider/analytics returns only this provider's data (negative test: another provider's data NOT included).
5. GET /provider/my-tours returns provider's tours only.
6. GET /provider/my-tours with cursor returns next page correctly.
7. Cache hit p95 < 50ms.
8. Concurrent GETs from same provider (10) → no stampede.
