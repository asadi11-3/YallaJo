# TASK 4 — Admin Dashboards

> **Owner:** Fadwa (Beginner→Intermediate) with Mohammad pairing first 2 days — **Hours:** 28h — **Hard deadline:** Sun **2027-02-21 17:00**
> **Earliest start:** Wed 2027-01-20
> **Endpoints:** 4 + ~3 cache-invalidation inbox handlers

---

## 1. Endpoint list

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/admin/dashboard` | `MustHavePermission(AdminDashboard, Read)` |
| 2 | GET | `/api/v1/admin/dashboard/revenue?from=&to=&granularity=` | same |
| 3 | GET | `/api/v1/admin/dashboard/bookings?from=&to=&granularity=` | same |
| 4 | GET | `/api/v1/admin/dashboard/users?from=&to=&granularity=` | same |

---

## 2. GET /admin/dashboard (Overview)

Returns a snapshot of platform health for the admin homepage. Cache 30s.

```csharp
public sealed record AdminDashboardOverviewDto(
    AdminRevenueSummaryDto Revenue,
    AdminBookingsSummaryDto Bookings,
    AdminUsersSummaryDto Users,
    AdminAlertsDto Alerts
);

public sealed record AdminRevenueSummaryDto(
    decimal TotalToday,
    decimal TotalThisMonth,
    decimal TotalThisYear,
    string PrimaryCurrency,
    int ProvidersWithRevenue,
    decimal AvgBookingValue
);

public sealed record AdminBookingsSummaryDto(
    int TotalToday,
    int TotalThisWeek,
    int TotalThisMonth,
    decimal ConversionRate,        // confirmed/total
    decimal CancellationRate,
    int PendingProviderConfirmations
);

public sealed record AdminUsersSummaryDto(
    int NewRegistrationsToday,
    int Active7d,
    int Active30d,
    int ProviderSignupsThisMonth,
    int ProvidersPending
);

public sealed record AdminAlertsDto(
    int FailedPayouts,           // queue from Finance
    int OutboxLag,               // unprocessed > 5min
    int OpenSupportTickets,
    int OverdueAdminReviews      // bookings PendingConfirmation > 20h, etc.
);
```

**Source data:** queries against PaymentSnapshot + BookingSnapshot + UserSnapshot read tables maintained via inbox handlers. **NEVER cross-module SELECT** — Analytics owns local snapshots populated by integration events.

---

## 3. GET /admin/dashboard/revenue?from=&to=&granularity=

Time-series data for charts. `granularity` = Day/Week/Month/Year (clamped to >=15 buckets, <=730 buckets — error `AdminDashboard.PeriodInvalid` outside).

```csharp
public sealed record AdminRevenueTimeSeriesDto(
    DashboardPeriod Period,
    IReadOnlyList<RevenueTimePointDto> Series,
    IReadOnlyList<RevenueByCurrencyDto> Currencies,
    IReadOnlyList<RevenueByProviderDto> TopProviders,
    IReadOnlyList<RevenueByCategoryDto> TopCategories
);

public sealed record RevenueTimePointDto(DateTime Bucket, decimal Gross, decimal Commission, decimal Refunded, decimal Net);
public sealed record RevenueByCurrencyDto(string Currency, decimal Total);
public sealed record RevenueByProviderDto(Guid ProviderId, string ProviderName, decimal Total);
public sealed record RevenueByCategoryDto(Guid CategoryId, string CategoryName, decimal Total);
```

**SQL:**
```sql
WITH buckets AS (
    SELECT 
        DATEADD(@granularity, DATEDIFF(@granularity, 0, p.CompletedAt), 0) AS Bucket,
        SUM(CASE WHEN p.Type = 'Booking' THEN p.Amount ELSE 0 END) AS Gross,
        SUM(p.CommissionAmount) AS Commission,
        SUM(CASE WHEN p.Type = 'Refund' THEN p.Amount ELSE 0 END) AS Refunded
    FROM analytics.PaymentSnapshots p
    WHERE p.CompletedAt >= @from AND p.CompletedAt < @to AND p.Status = 'Completed'
    GROUP BY DATEADD(@granularity, DATEDIFF(@granularity, 0, p.CompletedAt), 0)
)
SELECT Bucket, Gross, Commission, Refunded, (Gross - Commission - Refunded) AS Net FROM buckets ORDER BY Bucket;
```

`PaymentSnapshots` table populated by `FinancePaymentCompletedHandler` inbox (T6 documents the snapshot upkeep responsibility).

---

## 4. GET /admin/dashboard/bookings

Series of confirmed/cancelled/pending bookings over time + funnel metrics.

```csharp
public sealed record AdminBookingsTimeSeriesDto(
    DashboardPeriod Period,
    IReadOnlyList<BookingTimePointDto> Series,
    BookingFunnelDto Funnel
);

public sealed record BookingTimePointDto(
    DateTime Bucket,
    int Created,
    int Confirmed,
    int Cancelled,
    int Completed,
    decimal AvgValue
);

public sealed record BookingFunnelDto(
    int Total,
    int AwaitingPayment,
    int PendingConfirmation,
    int Confirmed,
    int Completed,
    int Cancelled,
    int Rejected,
    decimal ConversionRate,        // Completed / Total
    decimal AbandonmentRate        // AwaitingPayment expired / Total
);
```

Source: `analytics.BookingSnapshots` table (mirror of `booking.TourBookings` populated by Booking inbox events).

---

## 5. GET /admin/dashboard/users

```csharp
public sealed record AdminUsersTimeSeriesDto(
    DashboardPeriod Period,
    IReadOnlyList<UserTimePointDto> Series,
    UsersBreakdownDto Breakdown
);

public sealed record UserTimePointDto(
    DateTime Bucket,
    int NewRegistrations,
    int ActiveUsers,            // count from UserInteractions COUNT(DISTINCT UserId)
    int ProviderSignups
);

public sealed record UsersBreakdownDto(
    int TotalUsers,
    int TotalProviders,
    int VerifiedProviders,
    int VerifiedUsers,           // email-verified
    int ActiveLast30d
);
```

Source: `analytics.UserSnapshots` table (mirror of Auth.Users) + UserInteractions COUNT(DISTINCT).

---

## 6. ~3 cache-invalidation inbox handlers

| Inbox event | Cache tags to evict |
|---|---|
| `finance.payment.completed.v1` | `admin:dashboard:overview`, `admin:dashboard:revenue` |
| `booking.tour-booking.created.v1` | `admin:dashboard:overview`, `admin:dashboard:bookings` |
| `auth.user.registered.v1` | `admin:dashboard:overview`, `admin:dashboard:users` |

These ALSO update snapshot tables (`PaymentSnapshots`, `BookingSnapshots`, `UserSnapshots`) — combined inbox handler per source event.

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Snapshot tables (PaymentSnapshots, BookingSnapshots, UserSnapshots) + EF configs + migration `AnalyticsAddDashboardSnapshotTables` | 4 | 2027-01-25 |
| 2 | DashboardCache table + repo + EF + migration | 3 | 2027-01-28 |
| 3 | GET /admin/dashboard overview handler + 4 sub-queries | 5 | 2027-02-04 |
| 4 | GET /admin/dashboard/revenue + SQL + DTO + integration test | 5 | 2027-02-09 |
| 5 | GET /admin/dashboard/bookings + funnel calc + integration test | 4 | 2027-02-13 |
| 6 | GET /admin/dashboard/users + UserInteractions COUNT(DISTINCT) | 3 | 2027-02-17 |
| 7 | 3 inbox handlers for snapshot upkeep + cache invalidation | 3 | 2027-02-20 |
| 8 | Unit + integration tests (12+) | 1 | 2027-02-21 |
| **Total** | | **28h** | **Sun 2027-02-21** |

---

## 8. Acceptance tests (12+)

1. GET overview → 200 with all sections populated (zeros allowed).
2. GET revenue with from > to → 422 PeriodInvalid.
3. GET revenue 1-year span Day granularity → 365 buckets returned.
4. GET revenue 2-year span Day granularity → 422 PeriodInvalid.
5. GET bookings funnel — sum of statuses === Total.
6. Inbox payment.completed → PaymentSnapshots row appears.
7. Inbox payment.completed → cache `admin:dashboard:revenue` evicted; next GET refresh.
8. GET users with no UserInteractions → ActiveUsers=0.
9. GET revenue Week granularity → buckets aligned to Monday (or PreferredFirstDayOfWeek).
10. Cache hit p95 < 30ms (just deserialization).
11. Cache miss p95 < 500ms (cold query).
12. Concurrent GETs (50 simultaneous) → no stampede, cache holds.
