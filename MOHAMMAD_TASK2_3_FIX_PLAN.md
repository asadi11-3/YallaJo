# Mohammad — Task 2 & 3 Fix Plan

**Author:** Oracle review + codebase verification
**Date:** 2026-04-26
**Scope:** ContentTours · TourSchedule (Task 2A) · TourPricingTier (Task 2B) · Search/Featured/MyTours (Task 3)
**Verdict:** **Spec has 7 critical defects. Handlers were already implemented and inherit several of them.** Schema change required for one defect; rest are handler/spec/contract patches.

---

## 0. Executive Summary

| Status | Item |
|---|---|
| ✅ Already implemented | `TourSchedule.Create/Update/Delete` handlers, `TourPricingTier.Create/Update/Delete` handlers, `IContentToursOutboxWriter` + impl, integration event types, registry entries |
| ❌ Not implemented yet | Task 3 (Search/Suggest/Featured/MyTours/ToggleFeatured) — no handlers found |
| 🔴 Bug in shipped code | TourSchedule recurrence expansion silently drops rows (entity lacks `Date` column) |
| 🔴 Bug in shipped code | `Outcome.Conflict` returned for `ExpansionTooLarge` (spec promised 422; system can't return 422) |
| 🔴 Spec-vs-code mismatch | Team task plan says "write to `dbContext.OutboxMessages` directly" — handlers correctly use `IContentToursOutboxWriter` instead. **Spec doc is wrong.** |
| 🟠 Latent bugs | Idempotency key too weak; Adult-tier guard incomplete; no DB unique index on tier name; ChangeType is freeform string |
| 🟡 Doc gaps | PUT semantics for schedule undefined; concurrency error code unreachable; cache-key Accept-Language without translation entity |

**Effort estimate:**
- Schema migration + handler rewrite (Fix #1): **6–10 hrs**
- Other handler patches: **4–6 hrs**
- Task 3 implementation: per original WBS (24 hrs)
- Spec doc fixes: **2 hrs**

---

## 1. Verified Codebase Facts

| Claim | Verified | Evidence |
|---|---|---|
| `IContentToursOutboxWriter` exists | ✅ | `ContentTours.Application/Interfaces/IContentToursOutboxWriter.cs` — single method `Enqueue(IIntegrationEvent)` |
| Outbox writer is correctly used in shipped handlers | ✅ | `CreateTourScheduleCommandHandler.cs:93`, `CreateTourPricingTierCommandHandler.cs:64` both call `outbox.Enqueue(...)` |
| `Outcome` enum has NO 422 | ✅ | `YallaJo.SharedKernel.Domain/Abstractions/Results/Outcome.cs` — values: 200, 201, 400, 401, 403, 404, 409, 429, 499, 500 |
| `Tour` has `RowVersion` | ✅ | Via `AuditableEntity<TKey>` base — `[Timestamp] byte[] RowVersion` |
| `TourSchedule` and `TourPricingTier` have NO `RowVersion` | ✅ | Both inherit `BaseEntity` (not `AuditableEntity`) |
| `TourSchedule` has only `DayOfWeek + StartTime + EndTime` (no `Date`) | ✅ | `ContentTours.Domain/Entities/TourSchedule.cs:9-13` |
| `TourPricingTier` has BOTH `Money Price` AND `string Currency` | ✅ | `ContentTours.Domain/Entities/TourPricingTier.cs:13-14` |
| `OutboxMessage.Create(IIntegrationEvent)` factory exists | ✅ | `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessage.cs` — uses `IntegrationEventTypeRegistry.GetName(...)` |
| `IntegrationEventTypeRegistry` has both ContentTours events | ✅ | `content-tours.schedule.changed.v1` + `content-tours.pricing-tier.changed.v1` + `content-tours.tour.featured-changed.v1` |
| Task 3 handlers exist | ❌ | No `SearchTours`, `SuggestTours`, `ListFeaturedTours`, `ListMyTours`, `ToggleTourFeatured` handlers found |

---

## 2. Critical Fixes (🔴 must do before merge)

### FIX-1 — TourSchedule recurrence engine silently drops rows

**Severity:** 🔴 Critical · data loss
**Files affected:**
- `ContentTours.Domain/Entities/TourSchedule.cs`
- `ContentTours.Application/Commands/TourSchedule/CreateTourSchedule/CreateTourScheduleCommandHandler.cs`
- New EF migration

**The bug**

`TourSchedule` stores only `(TourId, DayOfWeek, StartTime, EndTime)`. Idempotency key in handler line 78–88:
```csharp
var existingKeys = existing.Select(s => (s.DayOfWeek, s.StartTime)).ToHashSet();
// ...
if (existingKeys.Contains((dow, start))) { skipped++; continue; }
```

Combined with the expansion engine that emits one candidate **per calendar day** (Daily/Custom) or **per matching weekday in window** (Weekly), this collapses ~30 calendar days down to ≤7 distinct `(DayOfWeek, StartTime)` rows. **Daily/Custom patterns lose 90%+ of intended rows silently.** Returns `Created=N, Skipped=M` to caller — caller has no idea their schedule is wrong.

Worse: overlap algorithm iterates `byDay[dow]` lists. Two Daily candidates for same `DayOfWeek` (e.g., next two Mondays) hit `b.Start < endA` immediately because they're identical → false `OverlapDetected` 409.

**Fix — pick ONE option**

#### Option A (Recommended) — Drop Once/Daily/Custom; ship weekly-only

Match the entity to reality. Patch:

1. **Spec change** (`Agents/ContentTours-team-tasks.md`): rewrite Task 2A B2 to:
   - `pattern` is removed from request shape
   - Request becomes `{ daysOfWeek: byte[], startTime, endTime?, isActive }`
   - Handler emits exactly one row per requested day (1..7 max)
   - `validFrom/validTo/customDates` REMOVED
   - 120-row cap REMOVED (max is 7)
   - `ExpansionTooLarge` error code REMOVED

2. **Handler simplification**: replace 100+ lines of expansion code with `cmd.DaysOfWeek.Select(d => Create(...))`.

3. **Idempotency key stays** `(DayOfWeek, StartTime)` — now correct.

4. **Booking module integration unchanged** — Booking instantiates real date slots from weekly templates at booking time.

**Why this is recommended:** Matches existing entity, no migration, simpler API, matches industry standard for tour scheduling (most tour operators publish weekly schedules; specific date overrides are a separate feature).

#### Option B — Keep recurrence; add `Date` column

If product genuinely needs per-date schedules:

1. **Migration** `AddTourScheduleDate`:
   ```csharp
   migrationBuilder.AddColumn<DateOnly>(
       name: "Date",
       table: "TourSchedules",
       type: "date",
       nullable: false,
       defaultValueSql: "CAST(GETUTCDATE() AS DATE)");
   migrationBuilder.CreateIndex(
       name: "IX_TourSchedules_TourId_Date_StartTime",
       table: "TourSchedules",
       columns: new[] { "TourId", "Date", "StartTime" },
       unique: true);
   ```

2. **Entity change** — add `public DateOnly Date { get; private set; }` and pass through `Create/Update`.

3. **Handler change**:
   - Idempotency key becomes `(TourId, Date, StartTime)`
   - Overlap check runs per `Date`, not per `DayOfWeek`
   - `DayOfWeek` becomes computed (`Date.DayOfWeek`) — drop the field, OR keep it as a denormalized read-optimization

4. **Existing data** — TourSchedule table is currently empty (greenfield). No backfill needed.

**Decision required from Tech Lead.** Default if no answer: **Option A**.

---

### FIX-2 — `ExpansionTooLarge` returns wrong HTTP status

**Severity:** 🔴 Critical · API contract violation
**File:** `CreateTourScheduleCommandHandler.cs:60-64`

**The bug**

Handler does:
```csharp
return Result.Fail<CreateTourScheduleResult>(
    Outcome.Conflict,                        // ← maps to HTTP 409
    new Error("TourSchedule.ExpansionTooLarge", ...));
```

Spec (Task 2A B2) declared this is **422 UnprocessableEntity**. `Outcome` enum has no 422. Either the enum extends or the spec changes.

**Fix — pick ONE**

#### Option A (Recommended) — Add 422 to `Outcome`

Patch `YallaJo.SharedKernel.Domain/Abstractions/Results/Outcome.cs`:
```csharp
public enum Outcome
{
    Ok = 200,
    Created = 201,
    Invalid = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    UnprocessableEntity = 422,   // ← NEW
    TooManyRequests = 429,
    Canceled = 499,
    ServerError = 500
}
```

Add `Result.UnprocessableEntity<T>(Error)` helper in `Result.cs` (mirror existing `Conflict<T>`). Handler then uses `Outcome.UnprocessableEntity`.

422 is the right semantic: the request was syntactically valid (passed validators) but its meaning is invalid in current state. Spec uses it for `OverlapDetected`, `ExpansionTooLarge`, `CustomDateOutOfRange`, `Tour.NoAdultPricingTier`. All are 422 in REST conventions.

#### Option B — Patch the spec; downgrade to 409 (or 400 for input shape)

Acceptable if Tech Lead doesn't want to touch SharedKernel. Update `agent-context.md` error catalog and Task 2A/2B/3 spec to use 409 for these.

**Decision required.** Default: **Option A** (no broken contracts; wider system benefit).

---

### FIX-3 — `ConcurrencyConflict` error code is unreachable for Schedule/Tier

**Severity:** 🔴 Critical · spec lies + silent overwrites
**Files:**
- Spec doc (Task 2A B6 + Task 2B B3)
- `TourSchedule.cs` / `TourPricingTier.cs` if RowVersion added

**The bug**

`TourSchedule.ConcurrencyConflict` and `TourPricingTier.ConcurrencyConflict` 409 codes are listed. Both entities are `BaseEntity` (no `RowVersion`). EF cannot detect write-write conflicts → no DbUpdateConcurrencyException → code never reachable. Last-write-wins silently overwrites concurrent updates.

**Fix — pick ONE per entity**

#### Option A (Recommended) — Remove the dead error codes

Update spec to remove `TourSchedule.ConcurrencyConflict` and `TourPricingTier.ConcurrencyConflict` from error catalogs. Document **last-write-wins** semantics for these non-aggregate child entities. Acceptable because:
- Schedules and tiers are owner-managed (one provider per tour, low concurrency)
- Mutations are infrequent
- Adding `RowVersion` requires migration + base-class change

#### Option B — Promote both to `AuditableEntity` and add migration

If business requires conflict detection:
- Change base class: `TourSchedule : AuditableEntity` and `TourPricingTier : AuditableEntity`
- This adds: `IsDeleted`, `DeletedAt`, `RowVersion`
- Spec explicitly says "no `IsDeleted`" — conflicts with `AuditableEntity`
- Would need a custom intermediate base class `ConcurrencyTrackedEntity : BaseEntity` with only `RowVersion`

Default: **Option A**.

---

### FIX-4 — Adult-tier invariant has UPDATE escape hatch

**Severity:** 🔴 Critical · invariant bypass + data corruption
**Files:**
- `ContentTours.Application/Commands/TourPricingTier/UpdateTourPricingTier/UpdateTourPricingTierCommandHandler.cs`
- Spec Task 2B B1.3

**The bug**

Spec guards Adult-tier on **delete and deactivate only**. UPDATE can:
- Rename `"Adult"` → `"Adults"` (or `"البالغ"`) on the last active Adult tier of a Pending/Approved tour
- Bypasses the invariant; tour now violates submit-gate

**Fix**

The invariant must be evaluated on the **post-mutation state**. Add a helper:

```csharp
private async Task<Result<T>?> EnsureAdultTierStillPresentAsync<T>(
    Guid tourId, TourStatus status, IEnumerable<Guid> excludedTierIds, CancellationToken ct)
{
    if (status is not (TourStatus.Pending or TourStatus.Approved)) return null;

    var hasAdult = await tierRepo.AnyAsync(
        t => t.TourId == tourId
          && t.IsActive
          && !excludedTierIds.Contains(t.Id)
          && t.Name.ToLower() == "adult",   // CI collation OR computed column
        ct);

    return hasAdult
        ? null
        : Result.Conflict<T>("TourPricingTier.AdultTierRequired");
}
```

Call from:
- **Update handler**: when changing `Name`, `IsActive`, or both — pass `excludedTierIds: [tier.Id]` so the simulation excludes the tier being mutated, then re-checks if the post-state still has an Adult.
- **Delete handler**: pass `excludedTierIds: [tier.Id]`
- **Deactivate** (PUT with `IsActive=false`): same as delete

Update spec B1.3 to read: "Last active Adult tier on a Pending/Approved tour cannot be deleted, deactivated, **or renamed away from 'Adult'**."

**Long-term fix (recommended, separate ticket):** introduce `ParticipantType` enum on the tier instead of magic string matching. Insulates against locale/typo issues.

---

### FIX-5 — Currency duplication on `TourPricingTier`

**Severity:** 🔴 Critical · data drift risk
**File:** `ContentTours.Domain/Entities/TourPricingTier.cs`

**The bug**

Entity stores both:
```csharp
public Money Price { get; private set; }      // value object — likely contains Currency too
public string Currency { get; private set; }  // separate redundant column
```

Update handler line ~85:
```csharp
Price    = price;          // price.Currency = "USD"
Currency = currency.ToUpperInvariant();  // could be "EUR" if not validated
```

If `Money.Currency` and the separate `Currency` field disagree, integration event uses one and SQL queries use the other.

**Fix — pick ONE**

#### Option A (Recommended) — Drop the redundant column

1. EF migration: drop `Currency` column (it's already encoded in `Price_Currency` via `Money` value object configuration).
2. Entity: remove `public string Currency` field; expose `public string Currency => Price.Currency;` as a passthrough.
3. Constructor: take only `Money price`, derive currency from it.
4. Handler: build `var price = new Money(cmd.Price, cmd.Currency.ToUpperInvariant());` then pass only `price`.
5. Verify `Money` value object has a Currency property — quick read of `YallaJo.SharedKernel.Domain/ValueObjects/Money.cs` to confirm.

#### Option B — Keep both, enforce sync via constructor

1. Constructor asserts `price.Currency == currency` (case-insensitive).
2. Update method same assertion.
3. Add EF check constraint on the column pair.

Option A is cleaner and removes a class of bugs.

---

### FIX-6 — Spec doc instructs the wrong outbox pattern

**Severity:** 🔴 Critical · would cause Clean Architecture violation if followed
**File:** `Agents/ContentTours-team-tasks.md` (Task 2 sections)

**The bug**

Spec says:
> "publish `TourPricingTierChangedIntegrationEvent` directly via outbox"
> "Write integration events directly via `dbContext.OutboxMessages.Add(...)` BEFORE `SaveChangesAsync`"

This would inject `ContentToursDbContext` into the Application layer — explicit ADR violation.

**The reality**

Handlers correctly use `IContentToursOutboxWriter.Enqueue(...)` (Application interface, Infrastructure impl). See `CreateTourPricingTierCommandHandler.cs:64`.

**Fix — spec doc only**

Patch all Task 2 sections of `Agents/ContentTours-team-tasks.md`:

```diff
- publish `TourPricingTierChangedIntegrationEvent` directly via outbox
+ publish `TourPricingTierChangedIntegrationEvent` via `IContentToursOutboxWriter.Enqueue(...)`

- Write integration events directly via `dbContext.OutboxMessages.Add(...)` BEFORE `SaveChangesAsync`
+ Stage integration events via `outbox.Enqueue(integrationEvent)` BEFORE `SaveChangesAsync`. The outbox row is added to the EF change tracker by the writer and committed atomically.
```

Also patch the example handler snippet in the spec to inject `IContentToursOutboxWriter outbox` instead of `dbContext`.

---

### FIX-7 — Search facets snapshot caps `total` accuracy

**Severity:** 🔴 Critical · pagination breaks
**Spec:** Task 3 SearchTours B3
**Status:** Not yet implemented

**The bug**

Spec collapses two distinct numbers:
- `items.total` (used for pagination — must be exact)
- Facet counts (used for filter UI — approximate OK)

The 5000-row snapshot for facets implies `total = facetRows.Count` capped at 5000 → page 251 onward broken.

**Fix**

Two queries on the same filtered `IQueryable`:
```csharp
var filtered = BuildFilteredQuery(...);

// Q1: exact total for pagination (no cap)
var total = await filtered.CountAsync(ct);

// Q2: facet rows (capped, approximate OK)
const int FacetSampleCap = 5000;
var facetRows = await filtered
    .Select(t => new FacetProjection(...))
    .Take(FacetSampleCap + 1)   // +1 to detect "is approximate"
    .ToListAsync(ct);

bool facetsApproximate = facetRows.Count > FacetSampleCap;
if (facetsApproximate) facetRows.RemoveAt(FacetSampleCap);

// Q3: page items (existing)
var pageItems = await filtered.OrderBy(...).Skip(skip).Take(take)...;

return new SearchToursResult(
    items: pageItems, total: total, ...,
    facets: ComputeFacets(facetRows),
    facetsAreApproximate: facetsApproximate);
```

Add `FacetsAreApproximate: bool` to response DTO. UI can show "approx." badge when `total > 5000`.

---

## 3. High-Priority Fixes (🟠)

### FIX-8 — Idempotency dedup loses information silently

**File:** `CreateTourScheduleCommandHandler.cs:84`

When `(DayOfWeek, StartTime)` matches but `EndTime` or `IsActive` differ, handler silently `skipped++`. Caller's intended schedule is NOT what's in DB.

**Fix:** When key matches but other fields differ, return `Conflict("TourSchedule.AlreadyExistsWithDifferentEndTime", { existing, requested })`. Caller can decide to PUT the existing row instead.

(Becomes moot if FIX-1 Option B picked, since key is per-Date.)

---

### FIX-9 — 90-day cap from `today` blocks legitimate future windows

**File:** `CreateTourScheduleCommandHandler.cs:48`
```csharp
var cap = today.AddDays(90);
```

A provider configuring summer schedule in spring (June schedule submitted in March) gets `validTo` truncated to ~mid-June.

**Fix:** Cap should be window-length, not absolute:
```csharp
const int MaxWindowDays = 90;
var validTo = cmd.ValidTo ?? validFrom.AddDays(MaxWindowDays);
var maxAllowed = validFrom.AddDays(MaxWindowDays);
if (validTo > maxAllowed) validTo = maxAllowed;
```

Removes `today.AddDays(90)` calculation entirely. Rule becomes: "you can configure up to 90 days starting from `validFrom`, which itself can be any future date."

(Becomes moot if FIX-1 Option A picked.)

---

### FIX-10 — Overlap interval semantics undocumented

**File:** `CreateTourScheduleCommandHandler.cs:196`

Algorithm uses `if (b.Start < endA)` — implies `[start, end)` half-open intervals. But:
- Spec doesn't state this anywhere
- A row `[10:00, 13:00]` followed by `[13:00, 14:00]` is OK (no overlap)
- A row `[10:00, 13:00]` followed by `[13:00, 13:00]` — what now? The constructor blocks `EndTime <= StartTime`, so 13:00→13:00 can't be created

**Fix:** Add to spec Task 2A B3:
> Intervals are half-open `[StartTime, EndTime)`. `EndTime == null` is treated as `TimeOnly.MaxValue` (open-ended for the entire day). Two rows on the same `DayOfWeek` overlap iff `b.StartTime < (a.EndTime ?? TimeOnly.MaxValue)`.

---

### FIX-11 — Tier name uniqueness has no DB enforcement

**File:** `CreateTourPricingTierCommandHandler.cs:48`

In-memory check `existingTiers.Any(t => t.Name.Equals(cmd.Name, OrdinalIgnoreCase))` races with concurrent POST. Two requests hit the duplicate window → both pass the check → both insert.

**Fix:**
1. EF migration: add unique index `IX_TourPricingTiers_TourId_NameUpper` on `(TourId, UPPER(LTRIM(RTRIM(Name))))`. SQL Server supports filtered/computed indexes.
2. Wrap insert in `try { ... } catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))`. Translate to `TourPricingTier.NameConflict` 409.
3. Keep the in-memory pre-check as a fast path (saves a round-trip in the common case).

---

### FIX-12 — Tour currency change vs existing tiers undefined

**Cross-cutting:** Task 1 (Tour.Update) and Task 2B

If Mahmoud's `Tour.Update` allows changing `Currency`, every existing `TourPricingTier` becomes mismatched. Spec doesn't say what happens.

**Fix — pick ONE in Task 1 spec:**

- **Option A (Recommended):** Block `Tour` currency change once any `TourPricingTier` exists. Return `Tour.CurrencyLockedByPricingTiers` 409.
- **Option B:** Cascade-update all tiers in the same transaction. Add a domain method `Tour.ChangeCurrency(newCurrency)` that loads + updates tiers. More complex; not recommended.

Coordinate with Mahmoud (Task 1 owner).

---

### FIX-13 — `Accept-Language` in pricing cache key without translation entity

**Spec:** Task 2B B4 cache key = `ct:tour-pricing:{tourId}:active={activeOnly}:lang:{Accept-Language}`

There is no `TourPricingTierTranslation` entity. Tier `Name` and `Description` are not localized. Per-language cache keys multiply storage with identical payloads.

**Fix — pick ONE:**

- **Option A (Recommended):** Drop `lang:{Accept-Language}` from key. Confirm tier names are intentionally not translated (use English business vocabulary like `"Adult"`, `"Group 10+"`).
- **Option B:** Add `TourPricingTierTranslation` entity (mirrors `TourTranslation`). New migration, new repo, EntityTranslation orchestrator hook. Significant work — defer to next sprint.

---

### FIX-14 — `ChangeType` is a fragile freeform string

**Files:**
- `ContentTours.Contracts/TourScheduleChangedIntegrationEvent.cs`
- `ContentTours.Contracts/TourPricingTierChangedIntegrationEvent.cs`

Consumers (Finance, Booking, Analytics) will hard-code string comparisons. Typos drift silently.

**Fix:** Replace `string ChangeType` with an enum in Contracts:
```csharp
namespace ContentTours.Contracts;

public enum TourEntityChangeType : byte
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Deactivated = 4
}
```
Update both event records. Consumers get compile-time safety.

---

### FIX-15 — Search ranking formula not verified EF-translatable

**Spec:** Task 3 SearchTours B2. Not implemented.

`Math.Log10(BookingCount + 1)` and `1.0 / (1 + DaysSinceCreated / 30.0)` may or may not translate depending on EF Core 9 SQL Server provider version.

**Fix — implement defensively:**

1. Build prototype handler. Run against real SQL Server with `dbContext.Database.Log` enabled. Verify the SELECT contains `LOG10(...)` and `DATEDIFF(day, ...)`.
2. If translation fails: fall back to **stored computed column** `Score` recomputed nightly OR move ranking to in-memory after `.Take(500)` (lossy but works).
3. Add integration test `SearchToursHandler_ScoreFormula_TranslatesToSql` that asserts no `EvaluationFailedException` is thrown and the SQL contains `LOG`.

---

### FIX-16 — `LIKE '%token%'` is acknowledged CPU bomb but spec must note v1 limit

**Spec:** Task 3 SearchTours.

Each token becomes `WHERE Name LIKE '%t%'` — full table scan. At 100k tours and 10 RPS, DB CPU dies.

**Fix:**
1. Spec: add an `### Operational Limits` section: "v1 search uses LIKE '%...%' patterns. Acceptable up to 50k Tours and 10 search RPS. Beyond that, migrate to SQL Server full-text search via `AddTourSearchDocument` migration (already planned in team task §Schema migrations)."
2. Add rate limit on the endpoint: `[EnableRateLimiting("search")]` at 10 req/s per IP.
3. Add `SearchDocument` migration to the sprint's contingency plan (don't ship in v1, but be ready).

---

### FIX-17 — Suggest prefix match must not break index usage

**Spec:** Task 3 SuggestTours.

`Tour.Name.StartsWith(q, OrdinalIgnoreCase)` translates to `WHERE Name LIKE 'foo%'` only if collation is case-insensitive (it is — `SQL_Latin1_General_CP1_CI_AS`). Avoid `Name.ToLower() == ...` patterns which prevent index seeks.

**Fix:** When implementing, write:
```csharp
.Where(t => EF.Functions.Like(t.Name, $"{q}%"))
```
not:
```csharp
.Where(t => t.Name.ToLower().StartsWith(q.ToLower()))  // ← breaks index
```

Add the migration `AddTourNamePrefixIndex` (not deferred — Mohammad ships it):
```csharp
migrationBuilder.CreateIndex(
    name: "IX_Tours_Name",
    table: "Tours",
    column: "Name");
```

---

### FIX-18 — `ListMyTours` must paginate at SQL level

**Spec:** Task 3 ListMyTours.

Easy to write `await tours.ToListAsync(ct).Skip(skip).Take(take)` which loads everything into memory. For a provider with 1000 tours, that's bad.

**Fix:** Spec must enforce: handler builds `IQueryable<Tour>`, calls `CountAsync(ct)` for `total`, then `.Skip(skip).Take(take).Select(SummaryProjection).ToListAsync(ct)` — all in SQL.

Add unit test `ListMyToursHandler_DoesNotLoadAllRowsIntoMemory` that intercepts EF SQL and asserts `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY` is present.

---

### FIX-19 — `ToggleFeatured` cache invalidation missing owner's MyTours tag

**Spec:** Task 3 ToggleTourFeatured B8.

Provider's `/provider/my-tours` view shows `IsFeatured` per tour. Toggling Feature on a tour doesn't bust `my-tours:{ownerUserId}`. Stale data for 2 minutes.

**Fix:** Spec invalidation list becomes:
```
tours:featured
tour:{id}
tours:list
tours:search
my-tours:{tour.CreatedByUserId}    ← NEW
```

Handler must load Tour first (already needed for the `Status==Approved` check), so `CreatedByUserId` is available.

---

## 4. Medium Fixes (🟡)

### FIX-20 — `UpdateTourSchedule` PUT request shape undefined

**Spec:** Task 2A endpoint #14 — only POST (recurrence) shape is defined.

What does PUT update? Single row's StartTime/EndTime? Day? Active flag? **Spec is silent.** Without definition, handler exists but its DTO contract is unknown to consumers.

**Fix:** Add to spec Task 2A:
> ### PUT Schedule — Shape
> ```json
> { "dayOfWeek": 1, "startTime": "09:00:00", "endTime": "13:00:00", "isActive": true }
> ```
> All fields required (full replace, not patch). Re-runs overlap validation including this row's NEW position. Returns 200 with updated DTO. Returns 422 `TourSchedule.OverlapDetected` if new position overlaps another row.

(Existing handler in `UpdateTourScheduleCommandHandler.cs` should be checked against this — likely already implements something close.)

---

### FIX-21 — `DayOfWeek` byte storage vs enum readability

**File:** `TourSchedule.cs:10`

Storing `byte DayOfWeek` requires every consumer to remember "0 = Sunday". Magic numbers in code.

**Fix (low priority, post-sprint):**

1. Domain: `public DayOfWeek DayOfWeek { get; private set; }` (uses `System.DayOfWeek`).
2. EF config: `.HasConversion<byte>()` — storage stays `tinyint`.
3. API DTOs: still accept `byte` 0–6 to keep wire format stable.

---

### FIX-22 — `TimeOnly` EF mapping needs explicit conversion

**Spec:** Task 2A B1.3.

EF Core 9 SQL Server provider supports `TimeOnly` natively (maps to `time(7)`). Earlier docs sometimes recommend `TimeSpan` conversion. Verify EF config explicitly.

**Fix:** Add to `TourScheduleConfiguration.cs`:
```csharp
builder.Property(s => s.StartTime).HasColumnType("time(0)");
builder.Property(s => s.EndTime).HasColumnType("time(0)").IsRequired(false);
```
`time(0)` (second precision) is enough for tour schedules; reduces storage. If `time(7)` (default) is OK, omit the call.

---

### FIX-23 — Token AND-semantic + score tuning conflict

**Spec:** Task 3 B1 + B2.

If all tokens must match (AND semantic), then "tokens matched in Name" count is constant per row — same N for every row. Score becomes:
```
SCORE = 3*N (constant) + 1*N (constant) + ratingBoost + popularityBoost + recencyBoost
```
The 3× and 1× weights effectively become irrelevant for ranking. Only ratings/popularity/recency differentiate. Defeats the purpose of weighted token matching.

**Fix — pick ONE:**

- **Option A (simpler):** Keep AND filter, drop the per-token-count terms from score. Rank purely by `ratingBoost + popularityBoost + recencyBoost`. Update spec.
- **Option B (richer):** Switch to OR filter with minimum-match threshold (e.g., must match ≥1 token). Token-count terms now genuinely differentiate. More complex query, but better relevance.

Default: **Option A** for v1.

---

### FIX-24 — `providerUserId` silent ignore should be observable

**Spec:** Task 3 ListMyTours B7.

Silently ignoring the query param protects against IDOR enumeration but confuses legitimate clients debugging integrations.

**Fix:** Add to handler:
```csharp
if (cmd.ProviderUserId.HasValue && cmd.ProviderUserId.Value != currentUser.UserId.Value
    && !currentUser.IsInRole("Admin"))
{
    logger.LogInformation(
        "ListMyTours: providerUserId={Requested} ignored for non-admin user {Actual}",
        cmd.ProviderUserId, currentUser.UserId);
}
```
Single Information log line — no PII beyond IDs. Visible in operator logs but not 403'd to client.

---

## 5. Low / Nit Fixes (🟢)

### FIX-25 — Integration event `NewPrice` misleading on Delete
On `ChangeType=Deleted`, the field is "last known price", not "new price". Document or rename.

### FIX-26 — Featured limit hardcoded to 20
Move to `IOptions<ContentToursOptions>` so business can tune without code change.

### FIX-27 — Add `CreatedByUserId` cross-bust on Tour ownership transfer
Future-proofing: if admin reassigns a Tour to another provider, must bust BOTH old and new `my-tours:{userId}` tags. Document in spec.

---

## 6. Open Questions for Tech Lead

Before Mohammad implements Task 3 (or merges Task 2 fixes), Tech Lead must rule on:

| # | Question | Default if no answer |
|---|---|---|
| Q1 | TourSchedule storage: weekly-pattern rows (Option A) OR per-date rows with new `Date` column (Option B)? | **Option A** (matches existing entity, no migration) |
| Q2 | HTTP 422 support: extend `Outcome` enum (Option A) OR downgrade spec to 409 (Option B)? | **Option A** (semantic correctness, broader system benefit) |
| Q3 | `TourPricingTier` currency: drop redundant `Currency` column (Option A) OR enforce sync (Option B)? | **Option A** (eliminate drift class) |
| Q4 | `TourSchedule`/`TourPricingTier` concurrency: remove `ConcurrencyConflict` codes (Option A) OR add custom `ConcurrencyTrackedEntity` base (Option B)? | **Option A** (low-concurrency entities) |
| Q5 | Tour currency change with existing tiers: block (Option A) OR cascade (Option B)? | **Option A** (block — ask Mahmoud to enforce in Task 1) |
| Q6 | `TourPricingTier` translations: drop `Accept-Language` from cache key (Option A) OR ship `TourPricingTierTranslation` entity (Option B)? | **Option A** (defer translations to next sprint) |
| Q7 | Search ranking AND vs OR: simplify to ratings/recency-only with AND (Option A) OR switch to OR with token-count weighting (Option B)? | **Option A** (v1 simplicity) |
| Q8 | "Adult" magic string vs `ParticipantType` enum? | Magic string for v1; enum in next sprint |
| Q9 | Add `IX_Tours_Name` index migration in this sprint, or defer? | **Add this sprint** (cheap, blocks future hot fixes) |

---

## 7. Spec Document Patches (concrete diffs)

### Patch A — `Agents/ContentTours-team-tasks.md` Task 2 outbox pattern

Replace every occurrence of:
```
publish ... directly via outbox
```
with:
```
publish ... via IContentToursOutboxWriter.Enqueue(...)
```

Replace the example handler block:
```diff
 public async Task<Result<CreateTourPricingTierResult>> Handle(...)
 {
     // 1. Load parent, ownership + currency checks
     // 2. Create entity
-    // 3. Write outbox row
-    var integrationEvent = new TourPricingTierChangedIntegrationEvent(...);
-    dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
+    // 3. Stage outbox event (Application layer — uses interface)
+    outbox.Enqueue(new TourPricingTierChangedIntegrationEvent(...));
     // 4. Commit (atomic with outbox)
     await unitOfWork.SaveChangesAsync(ct);
     // 5. Cache invalidation AFTER save
 }
```

Update DI list: replace `dbContext` with `IContentToursOutboxWriter outbox`.

### Patch B — Task 2A B2 (recurrence)

If Q1 Option A picked: rewrite the entire B2 section to spec weekly-only as in FIX-1 Option A.

If Q1 Option B picked: add note "requires `AddTourScheduleDate` migration. Idempotency key becomes `(TourId, Date, StartTime)`."

### Patch C — Task 2A B3 (overlap)

Add interval semantics paragraph from FIX-10.

### Patch D — Error catalogs

If Q2 Option A: keep 422 codes as-is, add note "requires `Outcome.UnprocessableEntity` PR before any of these codes returns the right status."

If Q2 Option B: rewrite all `422` cells to `409` (or `400` for shape errors).

If Q4 Option A: remove `TourSchedule.ConcurrencyConflict` and `TourPricingTier.ConcurrencyConflict` rows.

### Patch E — Task 2B B1.3 Adult-tier rule

Append: ", or **renamed to a non-Adult name**."

### Patch F — Task 2B B4 cache key

If Q6 Option A: change to `ct:tour-pricing:{tourId}:active={activeOnly}` (no lang).

### Patch G — Task 3 SearchTours B3

Add `facetsAreApproximate: bool` to response shape. Add separate `total = await filtered.CountAsync(ct)` step to algorithm.

### Patch H — Task 3 ToggleTourFeatured B8

Add `my-tours:{tour.CreatedByUserId}` to invalidation list.

---

## 8. Implementation Checklist for Mohammad

### Phase 1 — Resolve open questions (1 day, blocks all else)
- [ ] Get Tech Lead answers to Q1–Q9 in §6
- [ ] Update spec doc with chosen options (apply patches §7)

### Phase 2 — Critical handler fixes (1–2 days, behind Phase 1)
- [ ] Apply FIX-1 (recurrence rewrite OR schema + handler rewrite per Q1 answer)
- [ ] Apply FIX-2 (HTTP status fix per Q2 answer; possibly small SharedKernel PR)
- [ ] Apply FIX-3 (remove dead error codes OR add new base class per Q4 answer)
- [ ] Apply FIX-4 (Adult tier guard on UPDATE)
- [ ] Apply FIX-5 (drop redundant Currency column per Q3 answer + migration)
- [ ] Apply FIX-11 (DB unique index on tier name + migration)
- [ ] Apply FIX-14 (ChangeType enum in Contracts — coordinate with Finance team if events already consumed)

### Phase 3 — Task 3 implementation (per WBS, 24 hrs)
- [ ] `ListMyTours` + `ListFeaturedTours` (apply FIX-18 SQL pagination)
- [ ] `ToggleTourFeatured` (apply FIX-19 cache invalidation)
- [ ] `SuggestTours` (apply FIX-17 EF.Functions.Like + ship `IX_Tours_Name` migration)
- [ ] `SearchTours` tokenizer + ranking (apply FIX-15 EF translation verification)
- [ ] `SearchTours` facets (apply FIX-7 separate `CountAsync` + approximate flag + FIX-23 simplified scoring per Q7)
- [ ] Endpoint wiring + Swagger + smoke tests

### Phase 4 — Validation (0.5 day)
- [ ] `dotnet build` clean
- [ ] All acceptance test scenarios from Task 2 B5 + Task 3 B11 pass
- [ ] Smoke test in Swagger: create tour → create schedules (overlap rejected) → create Adult tier (mandatory) → submit tour
- [ ] Verify outbox rows are created (`SELECT TOP 10 * FROM ContentTours.OutboxMessages ORDER BY OccurredOnUtc DESC`)
- [ ] Verify cache invalidation actually busts (manual: hit GET, mutate, hit GET again, observe fresh data)

### Phase 5 — PR + review (0.5 day)
- [ ] Self-review against this fix plan
- [ ] PR description references this doc
- [ ] Tech Lead + Mahmoud (Task 1 owner) approval

---

## 9. Files Touched Summary

| Layer | File | Change |
|---|---|---|
| Domain | `ContentTours.Domain/Entities/TourSchedule.cs` | FIX-1 (depends on Q1), possibly FIX-21 |
| Domain | `ContentTours.Domain/Entities/TourPricingTier.cs` | FIX-5 (drop Currency field) |
| Application | `Commands/TourSchedule/CreateTourSchedule/...Handler.cs` | FIX-1 (rewrite expansion), FIX-2 (status), FIX-8, FIX-9 |
| Application | `Commands/TourSchedule/UpdateTourSchedule/...Handler.cs` | Verify against FIX-20 PUT shape |
| Application | `Commands/TourPricingTier/UpdateTourPricingTier/...Handler.cs` | FIX-4 (Adult guard on rename), FIX-11 (catch unique violation) |
| Application | `Commands/TourPricingTier/CreateTourPricingTier/...Handler.cs` | FIX-5 (single Money param), FIX-11 |
| Application | `Commands/TourPricingTier/DeleteTourPricingTier/...Handler.cs` | FIX-4 (post-state check) |
| Contracts | `ContentTours.Contracts/TourScheduleChangedIntegrationEvent.cs` | FIX-14 (enum) |
| Contracts | `ContentTours.Contracts/TourPricingTierChangedIntegrationEvent.cs` | FIX-14 (enum) |
| Infrastructure | New EF migration `DropTourPricingTierCurrencyColumn` | FIX-5 |
| Infrastructure | New EF migration `AddTourPricingTierNameUniqueIndex` | FIX-11 |
| Infrastructure | New EF migration `AddTourNameIndex` | FIX-17 |
| Infrastructure | New EF migration `AddTourScheduleDate` (only if Q1 = Option B) | FIX-1 |
| SharedKernel | `Outcome.cs` + `Result.cs` (only if Q2 = Option A) | FIX-2 |
| Spec | `Agents/ContentTours-team-tasks.md` | All §7 patches |
| New (Task 3) | `Queries/Tour/SearchTours/...Handler.cs` etc. | Implement per Phase 3 |

---

## 10. References

- Oracle review session: `ses_234a4f3bbffewvA9GNRVHL9Faf`
- Codebase verification session: `ses_2349a2015ffe1A4DNZEjz9m1vr`
- Spec: `Agents/ContentTours-team-tasks.md`
- Conventions: `Agents/agent-context.md`
- ADRs: `Agents/decisions/ADR-002-cqrs-mediatr.md`, `Agents/decisions/ADR-004-result-pattern.md`
- Architectural patterns: `ARCHITECTURAL_PATTERNS_FOR_MOHAMMAD.md`

---

**Sign-off:** This plan covers 27 distinct fixes across 7 critical, 12 high, 4 medium, 3 low items, plus 9 open questions for Tech Lead and 8 spec doc patches. Phase 1 (open questions) is the only blocker — once answered, remainder is straight-line work.
