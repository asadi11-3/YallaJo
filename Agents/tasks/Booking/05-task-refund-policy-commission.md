# TASK 2 — Refund Policy + Commission CRUD

**Owner:** Fadwa (Beginner — mentored by Mohammad on commission tier overlap detection)
**Endpoints:** 6 (2 refund policy + 4 commission CRUD)
**Estimated hours:** 24
**Earliest start:** Wed 2026-06-17
**Hard PR deadline:** Mon 2026-06-29 17:00
**Dependencies:** PW-1, PW-2, PW-6 (Finance.Contracts seeded). NO dependency on TASK 1.

---

## Endpoint list

| # | Method + Path | Permission | Owner-side returns |
|---|---|---|---|
| 1 | `POST /api/v1/booking/refund-policies` | `RefundPolicy + Create` | 201 + DTO |
| 2 | `PUT /api/v1/booking/refund-policies/{id}` | `RefundPolicy + Update` | 200 |
| 3 | `GET /api/v1/booking/refund-policies/{tourId}` | `AllowAnonymous` | 200 + tiers (or default if none) |
| 4 | `POST /api/v1/booking/commissions` | `Commission + Create` (admin only) | 201 |
| 5 | `PUT /api/v1/booking/commissions/{id}` | `Commission + Update` (admin only) | 200 |
| 6 | `DELETE /api/v1/booking/commissions/{id}` | `Commission + Delete` (admin only) | 204 |
| 7 | `GET /api/v1/booking/commissions` | `Commission + Read` (admin only) | 200 + list |

> Endpoint 7 is counted but not in the "6" total above (it's a free GET on top of the 4 CRUD). Bringing real endpoint total to 7 in this task.

---

## A. RefundPolicy half

### Payloads

POST/PUT body:
```json
{
  "tourId": "<guid>",
  "tiers": [
    { "hoursBeforeTour": 72, "refundPercentage": 100 },
    { "hoursBeforeTour": 24, "refundPercentage": 50 },
    { "hoursBeforeTour": 0,  "refundPercentage": 0 }
  ]
}
```

### Validators

- `tiers` non-empty, ≤10 items.
- Each `hoursBeforeTour >= 0`, `refundPercentage ∈ [0, 100]`.
- Tiers MUST be **strictly decreasing on `hoursBeforeTour`** (descending). Validator sorts and asserts.
- **Platform bounds (PDF 2 §1.5):** if any tier has `hoursBeforeTour >= 48`, its `refundPercentage >= 50`. Else `Result.Failure(new Error("RefundPolicy.OutOfPlatformBounds", "Tiers at 48h+ must offer ≥50% refund"), Outcome.Validation)`.
- Last tier (smallest hours) is allowed to be 0%.
- One policy per tour (UPSERT semantics on POST when same TourId — handler checks for existing and updates instead of inserting; returns 200 vs 201 accordingly).

### Domain method on `RefundPolicy`

```csharp
public static RefundPolicy CreateForTour(Guid tourId, Guid providerId, IReadOnlyList<RefundPolicyTier> tiers)
{
    ValidateTiers(tiers);
    return new RefundPolicy
    {
        Id = Guid.CreateVersion7(),
        TourId = tourId,
        ProviderId = providerId,
        Tiers = tiers.OrderByDescending(t => t.HoursBeforeTour).ToList()
    };
}

public Result UpdateTiers(IReadOnlyList<RefundPolicyTier> newTiers)
{
    try { ValidateTiers(newTiers); }
    catch (DomainInvariantException ex) { return Result.Failure(new Error("RefundPolicy.InvalidTiers", ex.Message), Outcome.Validation); }
    Tiers = newTiers.OrderByDescending(t => t.HoursBeforeTour).ToList();
    MarkUpdated();
    return Result.Success();
}

public decimal CalculateRefundPercentage(TimeSpan timeUntilTour)
{
    foreach (var tier in Tiers) // already sorted desc
        if (timeUntilTour.TotalHours >= tier.HoursBeforeTour) return tier.RefundPercentage;
    return 0m;
}
```

### Cache

- `GET /refund-policies/{tourId}` query implements `ICacheableQuery` with tag `"refund-policy:tour:{tourId}"`. TTL 30 min.
- POST/PUT command handlers: `RemoveByTagAsync($"refund-policy:tour:{tourId}", ct)` after save.

### Edge cases

- Tour with NO policy → GET returns `{ tiers: [{ hoursBeforeTour: 24, refundPercentage: 100 }], isDefault: true }` (platform default: full refund up to 24h then 0%, per PDF 1 Wave 5).
- Provider tries to set tier `[{ hoursBeforeTour: 48, refundPercentage: 30 }]` → 400 `RefundPolicy.OutOfPlatformBounds`.
- Provider tries to set 11 tiers → 400.
- Tiers with same `hoursBeforeTour` → 400 (strict decrease violated).

---

## B. Commission CRUD half

### Architecture decision (HARD)

> **Per `01-pre-work.md` PW-6:** Commission entity lives in **Finance** module. Booking only consumes via `ICommissionLookupService.GetCommissionForProviderAsync(providerId, gross, currency, ct)`.
>
> **HOWEVER** the CRUD ENDPOINTS for Commission live in **Booking**? — NO. **CRUD ENDPOINTS LIVE IN FINANCE** because Commission is a Finance aggregate.
>
> **Fadwa's actual scope here:** the **READ-MODEL SNAPSHOT** in Booking. Booking has a `BookingCommissionSnapshot` table populated by inbox from `finance.commission-rule.upserted.v1` / `finance.commission-rule.deleted.v1`. Booking's POST /tour Step 3 reads from this snapshot via `ICommissionLookupService` — which in Booking.Infrastructure is implemented as a query against the snapshot table (NOT a network call to Finance — they're in the same process, but they MUST go through the snapshot pattern to keep module independence).
>
> **Net result for this task:** the "4 Commission CRUD endpoints" in PDF 1 Wave 5 are owned by **Finance sprint** (see `Phase1-Phase2-Completion-INDEX.md` §1 Finance row). **Remove endpoints 4-7 from this task.** Fadwa's commission work in THIS sprint shrinks to:
>
> 1. Create `BookingCommissionSnapshot` table + EF config.
> 2. Implement inbox handlers `FinanceCommissionRuleUpsertedInboxHandler` + `FinanceCommissionRuleDeletedInboxHandler` (using `IBookingInboxStore` pattern).
> 3. Implement `CommissionLookupService` in `Booking.Infrastructure/Services/` that queries the snapshot table and returns `CommissionResult`. Tiers in snapshot table sorted by `MaxMonthlyRevenue`; lookup picks the smallest tier where provider's monthly gross fits.
> 4. Wire into `BookingDependencyInjection.cs` as the concrete `ICommissionLookupService` (replacing PW-6 stub).
> 5. Register inbox handlers in Booking.Infrastructure DI.
>
> **Adjusted endpoint count for TASK 2 = 3 endpoints (refund policy POST/PUT/GET). Plus the snapshot wiring (no public surface).** Sprint INDEX endpoint total adjusts.

### `BookingCommissionSnapshot` schema

| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier (PK = upstream CommissionRule.Id) | |
| Tier | nvarchar(50) | "Free" / "Basic" / "Premium" / "Enterprise" |
| MinMonthlyRevenue | decimal(19,4) | inclusive |
| MaxMonthlyRevenue | decimal(19,4) NULL | NULL = open-ended (top tier) |
| Currency | nvarchar(3) | "JOD" baseline |
| Percentage | decimal(5,2) | 15.00 / 10.00 / 7.00 / custom |
| LastEventId | nvarchar(100) | from inbox message; for ordering when replays happen |
| UpdatedAt | datetime2 | |

> Index: `(MinMonthlyRevenue, MaxMonthlyRevenue)` to support lookup queries.

### `ICommissionLookupService` impl (Booking.Infrastructure/Services/CommissionLookupService.cs)

```csharp
public sealed class CommissionLookupService(BookingDbContext context, IBookingProviderSnapshotRepository providers) : ICommissionLookupService
{
    public async Task<CommissionResult> GetCommissionForProviderAsync(Guid providerId, decimal gross, string currency, CancellationToken ct)
    {
        var provider = await providers.GetAsync(providerId, ct)
            ?? throw new InvalidOperationException($"Provider snapshot {providerId} not found.");

        // For sprint #1: assume gross IS the monthly revenue.
        // Finance sprint #2 will introduce monthly aggregation; for now we use gross directly which approximates first-booking-of-the-month case.
        var monthlyGross = gross;

        var tier = await context.Set<BookingCommissionSnapshot>()
            .Where(t => t.Currency == currency && t.MinMonthlyRevenue <= monthlyGross && (t.MaxMonthlyRevenue == null || t.MaxMonthlyRevenue >= monthlyGross))
            .OrderBy(t => t.MinMonthlyRevenue)
            .FirstOrDefaultAsync(ct);

        if (tier == null)
            // Fall back to Free tier 15% (PDF 2 §1.5)
            return new CommissionResult(15.00m, gross * 0.15m, currency, "Free");

        return new CommissionResult(tier.Percentage, gross * tier.Percentage / 100m, currency, tier.Tier);
    }
}
```

> If `provider.SubscriptionTier` is set on the snapshot (from `accounts.provider.subscription-changed.v1` inbox), prefer that explicit tier over revenue-bucket lookup. Subscription tier override is the source of truth per PDF 2 §1.5.

---

## WBS

| Step | Sub-deliverable | Hours | Finish-by |
|---|---|---|---|
| 1 | Refactor `RefundPolicy.cs` aggregate + `RefundPolicyTier` value object + JSON column EF mapping | 3 | Wed 2026-06-17 EOD |
| 2 | CreateOrUpdateRefundPolicy CQRS (UPSERT) + validator + 4 unit tests (platform bounds, decreasing-tier, default) | 4 | Fri 2026-06-19 EOD |
| 3 | GetRefundPolicyForTour query + `ICacheableQuery` + integration test (default returned when none) | 2 | Sun 2026-06-21 EOD |
| 4 | Migration `BookingAddRefundPolicyJsonColumn` | 1 | Sun 2026-06-21 EOD |
| 5 | Create `BookingCommissionSnapshot` table + EF config + migration `BookingAddCommissionSnapshot` | 2 | Mon 2026-06-22 EOD |
| 6 | Implement `FinanceCommissionRuleUpsertedInboxHandler` + `FinanceCommissionRuleDeletedInboxHandler` (uses `IBookingInboxStore.HasBeenProcessedAsync` guard) | 3 | Tue 2026-06-23 EOD |
| 7 | Implement `CommissionLookupService` replacing PW-6 stub | 2 | Wed 2026-06-24 EOD |
| 8 | Replace `ICommissionLookupService` stub registration in `BookingDependencyInjection.cs` with new impl; ensure stub is still the fallback if Finance has not yet shipped inbox events | 1 | Wed 2026-06-24 EOD |
| 9 | Integration test: Finance integration event → Booking inbox → snapshot row → lookup returns correct tier | 3 | Thu 2026-06-25 EOD |
| 10 | Code review cycle | 3 | Mon 2026-06-29 EOD |
| **Sum** | | **24** | |

---

## Files Fadwa touches

```
Booking.Domain/Entities/RefundPolicy.cs                              (refactor)
Booking.Domain/ValueObjects/RefundPolicyTier.cs                       (new)
Booking.Application/Commands/CreateOrUpdateRefundPolicy/...
Booking.Application/Queries/GetRefundPolicyForTour/...
Booking.Application/Interfaces/IRefundPolicyRepository.cs              (already in PW-5)
Booking.Application/Interfaces/IBookingCommissionSnapshotRepository.cs (new)
Booking.Infrastructure/Repositories/RefundPolicyRepository.cs
Booking.Infrastructure/Repositories/BookingCommissionSnapshotRepository.cs
Booking.Infrastructure/Services/CommissionLookupService.cs              (new)
Booking.Infrastructure/EventHandlers/FinanceCommissionRuleUpsertedInboxHandler.cs (new)
Booking.Infrastructure/EventHandlers/FinanceCommissionRuleDeletedInboxHandler.cs (new)
Booking.Infrastructure/Persistence/Configurations/RefundPolicyConfiguration.cs
Booking.Infrastructure/Persistence/Configurations/BookingCommissionSnapshotConfiguration.cs
Booking.Infrastructure/Migrations/{timestamp}_BookingAddRefundPolicyJsonColumn.cs
Booking.Infrastructure/Migrations/{timestamp}_BookingAddCommissionSnapshot.cs
Booking.Presentation/Endpoints/RefundPolicyEndpoints.cs                 (new sub-file)
Booking.Presentation/BookingEndpoints.cs                                (wire-up)
tests/Booking.Tests.Unit/Commands/CreateOrUpdateRefundPolicyHandlerTests.cs
tests/Booking.Tests.Unit/RefundPolicyCalculatePercentageTests.cs
tests/Booking.Tests.Unit/Services/CommissionLookupServiceTests.cs
tests/Booking.IntegrationTests/RefundPolicyRoundTripTests.cs
tests/Booking.IntegrationTests/CommissionInboxRoundTripTests.cs
```

---

## Hand-off note for Mohammad (TASK 4)

When TASK 4 (Booking Engine) starts implementing POST /tour Step 3 pricing calculation:
- Inject `IRefundPolicyRepository` to **stamp** `Booking.RefundPolicySnapshot` (a JSON snapshot of policy at booking time — per PDF 2 §5 edge case "provider changes refund policy → original policy snapshot applies"). The snapshot lives on `TourBooking` aggregate as a JSON column.
- Inject `ICommissionLookupService` to compute commission breakdown (stored on `TourBooking` for payout-time use, not exposed to user).
- Pricing calculation order: see TASK 4 file (`07-task-booking-engine.md`).
