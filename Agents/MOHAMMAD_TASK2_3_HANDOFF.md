# Mohammad — Task 2 & 3 Handoff Guide
## ContentTours · TourSchedule + TourPricingTier + Search/Featured/MyTours
**13 endpoints · Sprint window: W 2026-05-06 → M 2026-05-18**

> **Status as of handoff**: All 13 endpoints are implemented, tested at build level, and green.
> All known bugs from oracle review have been fixed. This guide tells you **what was built,
> why each decision was made, and what you still need to do before opening a PR.**

---

## 0. Before You Start — Hard Blockers

| Blocker | Owner | Due | What you need |
|---|---|---|---|
| **PW-1** — `TourStatus` enum migration (`Published → Approved` + add `Pending, Rejected`) | Mahmoud | Mon 2026-05-04 17:00 | Without this, `ToggleTourFeatured` and `AdultTierGuard` use `Published` as a stand-in. Code is pre-wired via `TourStatusExtensions` — see §10. |
| **Tour.Currency lock** | Mahmoud (Task 1 `Tour.Update`) | Before Task 2 PR | If `Tour.Update` allows changing `Currency` after pricing tiers exist, all tiers become currency-mismatched. Mahmoud must return `Tour.CurrencyLockedByPricingTiers` 409 when any `TourPricingTier` exists. |

**Do not open a PR for Task 2/3 until PW-1 is merged.**

---

## 1. Architecture Recap — What Pattern to Follow

Every command handler in this module follows this exact shape:

```csharp
public async Task<Result<T>> Handle(MyCommand cmd, CancellationToken ct)
{
    // 1. Load parent tour — always check IsDeleted
    var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
    if (tour is null || tour.IsDeleted)
        return Result.NotFound<T>("Tour.NotFound");

    // 2. Owner-or-admin check — always before any mutation
    if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
        return Result.Forbidden<T>("Tour.NotOwner");

    // 3. Business invariants (currency, name uniqueness, adult guard, overlap...)

    // 4. Mutate entity via domain method

    // 5. Stage outbox event BEFORE SaveChanges (atomic commit)
    outbox.Enqueue(new SomeIntegrationEvent(...));

    // 6. Save — use IContentToursUnitOfWork for non-aggregates
    await unitOfWork.SaveChangesAsync(ct);

    // 7. Cache invalidation AFTER successful save
    await cache.RemoveByTagAsync(..., ct);

    return Result.Created(...) or Result.Success(...);
}
```

**Never**:
- Inject `ContentToursDbContext` into an Application handler (use `IContentToursOutboxWriter`)
- Call `SaveChangesAsync` inside an entity or domain service
- Use `GetAllAsync()` then `.Skip().Take()` in memory — always paginate at SQL level

---

## 2. TourSchedule — What Was Built (Task 2A)

### 2.1 Endpoints

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 12 | GET | `/api/v1/tours/{id}/schedules` | Anonymous | `ListTourSchedulesQueryHandler` |
| 13 | POST | `/api/v1/tours/{id}/schedules` | `Tour.Update` | `CreateTourScheduleCommandHandler` |
| 14 | PUT | `/api/v1/tours/{id}/schedules/{scheduleId}` | `Tour.Update` | `UpdateTourScheduleCommandHandler` |
| 15 | DELETE | `/api/v1/tours/{id}/schedules/{scheduleId}` | `Tour.Update` | `DeleteTourScheduleCommandHandler` |

### 2.2 Domain Model

`TourSchedule : BaseEntity` — weekly template rows. **No `IsDeleted`, no `RowVersion`.**

```
TourSchedule
  ├── TourId        Guid
  ├── DayOfWeek     byte  (0=Sunday … 6=Saturday)
  ├── StartTime     TimeOnly
  ├── EndTime       TimeOnly?   (null = open-ended)
  └── IsActive      bool
```

**Key design decision**: Weekly-only model. Each row represents "every [DayOfWeek] at [StartTime]".
The Booking module instantiates real date/time slots at booking time. This matches the entity —
there is no `Date` column and no per-date expansion.

### 2.3 POST — Create Schedules (the tricky one)

**Request shape** (weekly-only, no recurrence patterns):

```json
POST /api/v1/tours/{id}/schedules
{
  "daysOfWeek": [1, 3, 5],     // byte[] — Mon=1, Wed=3, Fri=5. At least one required.
  "startTime": "09:00:00",
  "endTime":   "13:00:00",     // nullable — null means open-ended
  "isActive":  true
}
```

**What the handler does**:
1. Deduplicates `DaysOfWeek` (caller may send `[1, 1, 3]` → treated as `[1, 3]`)
2. Loads existing active schedules for this tour
3. Checks overlap across all `(DayOfWeek, StartTime, EndTime)` combinations using `TourScheduleOverlapChecker.Check(existing, candidates)` — intervals are **half-open `[Start, End)`**, `EndTime=null` treated as `TimeOnly.MaxValue`
4. Idempotency: if `(DayOfWeek, StartTime, EndTime)` **exactly** match an existing row → skip (count as "skipped"). If `StartTime` matches but `EndTime` differs → return `422 TourSchedule.AlreadyExistsWithDifferentEndTime` with the existing row's ID (caller should PUT instead)
5. Creates at most 7 rows (one per unique weekday)
6. Returns `{ "created": N, "skipped": M }` — HTTP 201 if `created > 0`, HTTP 200 if `created == 0`

### 2.4 Overlap Validation Algorithm

Shared helper: `ContentTours.Application/Commands/TourSchedule/Common/TourScheduleOverlapChecker.cs`

```
For each DayOfWeek, sort all rows by StartTime.
For adjacent pairs (a, b):
    endA = a.EndTime ?? TimeOnly.MaxValue
    if b.StartTime < endA → OVERLAP (422 TourSchedule.OverlapDetected)
```

Used by both **Create** (validates existing + candidates together) and **Update** (validates existing minus current row + proposed new position).

### 2.5 DELETE — Booking Guard

Before deleting, handler calls:
```csharp
var futureBookings = await bookingCountService
    .GetFutureBookingCountForScheduleAsync(scheduleId, ct);
if (futureBookings > 0)
    return Result.Fail(Outcome.Conflict,
        new Error("TourSchedule.DeleteBlocked", $"{futureBookings} future booking(s)..."));
```

`IScheduleBookingCountService` is in `ContentTours.Application/Interfaces/`. The Infrastructure registers `NoOpScheduleBookingCountService` (always returns 0) via DI. The Booking module replaces this stub in a future sprint.

### 2.6 Cache

| Operation | Tags busted |
|---|---|
| POST/PUT/DELETE | `tour-schedules:{tourId}` + `tour:{tourId}` |
| GET (ListTourSchedules) | Cached 5 min, key `ct:tour-schedules:{tourId}:active={activeOnly}`, tags `tour-schedules:{tourId}`, `tour:{tourId}` |

---

## 3. TourPricingTier — What Was Built (Task 2B)

### 3.1 Endpoints

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 16 | GET | `/api/v1/tours/{id}/pricing` | Anonymous (owner/admin see inactive too) | `ListTourPricingTiersQueryHandler` |
| 17 | POST | `/api/v1/tours/{id}/pricing` | `Tour.Update` | `CreateTourPricingTierCommandHandler` |
| 18 | PUT | `/api/v1/tours/{id}/pricing/{tierId}` | `Tour.Update` | `UpdateTourPricingTierCommandHandler` |
| 19 | DELETE | `/api/v1/tours/{id}/pricing/{tierId}` | `Tour.Update` | `DeleteTourPricingTierCommandHandler` |

### 3.2 Domain Model

`TourPricingTier : BaseEntity` — **No `IsDeleted`, no `RowVersion`.**

```
TourPricingTier
  ├── TourId           Guid
  ├── Name             string (max 200, case-insensitive unique per tour)
  ├── Description      string? (max 500)
  ├── Price            Money (Amount + Currency embedded)
  ├── Currency         string  ← COMPUTED: returns Price.Currency, no DB column
  ├── ParticipantType  ParticipantType (byte enum — see §3.3)
  ├── MinParticipants  int (≥1)
  ├── MaxParticipants  int? (> MinParticipants when set)
  ├── IsActive         bool
  └── Translations     IReadOnlyCollection<TourPricingTierTranslation>
```

**Key decisions**:
- `Currency` is a computed passthrough (`=> Price.Currency`). No separate DB column. EF config: `builder.Ignore(x => x.Currency)`.
- `ParticipantType` enum replaces the fragile `"Adult"` magic-string check (see §3.3).

### 3.3 ParticipantType Enum

```csharp
// ContentTours.Domain/Enums/ParticipantType.cs
public enum ParticipantType : byte
{
    Adult   = 1,   // ← THE MAGIC VALUE — submit gate requires at least one active Adult tier
    Child   = 2,
    Senior  = 3,
    Student = 4,
    Infant  = 5,
    Group   = 6,
    Family  = 7,
    Other   = 255
}
```

`TourPricingTier.IsAdult` is now `=> ParticipantType == ParticipantType.Adult` — no string comparison.

**API**: clients send `"participantType": 1` (Adult) or `"participantType": 2` (Child) etc. Validator enforces `IsInEnum()` — value 0 is rejected.

### 3.4 Adult-Tier Guard

Rule: **The last active Adult tier on a non-Draft tour cannot be deleted, deactivated, or retyped.**

Enforced in `ContentTours.Application/Commands/TourPricingTier/Common/AdultTierGuard.cs`:

```csharp
// Fires on DELETE
if (tier.IsAdult) → run guard

// Fires on PUT — also catches retype and deactivation:
var isChangingAwayFromAdult = tier.IsAdult && cmd.ParticipantType != ParticipantType.Adult;
var isDeactivatingAdult     = !cmd.IsActive && tier.IsAdult;
if (isChangingAwayFromAdult || isDeactivatingAdult) → run guard
```

Guard logic:
```
if tour.Status.IsEditable() → allow (Draft tours are always editable)
if any OTHER active Adult tier exists → allow
else → 409 TourPricingTier.AdultTierRequired
```

`tour.Status.IsEditable()` is from `TourStatusExtensions` — currently maps to `Draft`. After PW-1 it should also include `Rejected` (provider resubmitting). **Update `TourStatusExtensions.IsEditable()` after PW-1 runs.**

### 3.5 Currency Match

Every tier's currency must match the parent tour's `Currency`:
```csharp
if (!cmd.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
    return Result.Invalid("TourPricingTier.CurrencyMismatch", ...);
```

**Mahmoud must block `Tour.Update` from changing `Currency` once any `TourPricingTier` exists** (see §0). Without this guard, existing tiers become silently mismatched.

### 3.6 TourPricingTierTranslation

New entity for localised tier names/descriptions.

```
TourPricingTierTranslation
  ├── TourPricingTierId  Guid (FK → TourPricingTiers, CASCADE DELETE)
  ├── LanguageCode       string  (e.g. "en", "ar", "ar-jo")
  ├── Name               string (max 200)
  └── Description        string? (max 500)
```

Unique index: `(TourPricingTierId, LanguageCode)`.

`ListTourPricingTiersQueryHandler` resolves localised names:
1. Exact `languageCode` match (e.g. `"ar-jo"`)
2. Neutral fallback (`"ar"`)
3. Fallback to `tier.Name` / `tier.Description` (English originals)

Language code comes from query param `?lang=` or `Accept-Language` header.

The `LanguageActivatedIntegrationEventHandler` creates stub translations for all existing tiers when a new language is activated (copies the English name as starting point for translator).

### 3.7 Cache

| Operation | Tags busted |
|---|---|
| POST/PUT/DELETE | `tour-pricing:{tourId}` + `tour:{tourId}` |
| GET (ListTourPricingTiers) | Cached 10 min, key `ct:tour-pricing:{tourId}:active={activeOnly}:lang:{lang}`, tags `tour-pricing:{tourId}`, `tour:{tourId}` |

---

## 4. Search / Featured / MyTours — What Was Built (Task 3)

### 4.1 Endpoints

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 20 | GET | `/api/v1/tours/search` | Anonymous | `SearchToursQueryHandler` |
| 21 | GET | `/api/v1/tours/search/suggest` | Anonymous | `SuggestToursQueryHandler` |
| 22 | GET | `/api/v1/tours/featured` | Anonymous | `ListFeaturedToursQueryHandler` |
| 23 | GET | `/api/v1/tours/provider/my-tours` | `Tour.ReadOwn` | `ListMyToursQueryHandler` |
| 24 | PATCH | `/api/v1/tours/admin/{id}/feature` | `Tour.Feature` | `ToggleTourFeaturedCommandHandler` |

### 4.2 SearchTours

**Tokenizer** (`SearchTokenizer.cs`):
- Trim → lowercase → split on `\s+` → drop tokens < 2 chars → dedup → cap at 10

**AND semantics**: all tokens must appear in `Name` OR `Description`. Any tour missing a token is excluded.

**Filter pipeline** (EF-translatable, all pushed to SQL):
- `placeId`, `priceMin/Max`, `difficulty` (enum compare, not string), `durationMinutes Min/Max`
- `isChildFriendly`, `isAccessible`, `isInstantBooking`
- `hasDiscount` → `DiscountValidFrom <= UtcNow AND DiscountValidTo > UtcNow`
- `minRating`

**Three SQL queries per request** (all on same `IQueryable<Tour>` before pagination):
1. `CountAsync()` — exact total for pagination
2. `.Take(5001)` facet projection — approximate if > 5000 rows (`FacetsAreApproximate: true` in response)
3. `.Skip().Take()` page items

**Sorting**:
- `PriceAsc/Desc` — uses `SalePrice ?? BasePrice.Amount`
- `RatingDesc` — `AverageRating DESC, ReviewCount DESC`
- `PopularityDesc` — `BookingCount DESC, CreatedAt DESC`
- `Newest` — `CreatedAt DESC`
- `Relevance` (default when `q` set) — uses `PopularityDesc` as v1 proxy

**Caching**: SHA1 hash over JSON-serialized `SearchToursRequest`. 2-min TTL. Tags `["tours:search", "tours:list"]`.

**IMPORTANT — v1 performance limit**: `Name.Contains(token)` translates to `LIKE '%token%'` — full scan. Acceptable up to ~50k tours and ~10 RPS. Beyond that: use `AddTourSearchDocument` migration (persisted computed column) or SQL Server full-text search. This is logged as tech debt.

### 4.3 SuggestTours

```csharp
.Where(t => EF.Functions.Like(t.Name, prefix + "%"))  // index-friendly LIKE 'prefix%'
.OrderByDescending(t => t.BookingCount)
.ThenBy(t => t.Name)
.Take(10)
```

`IX_Tours_Name` index is in the migration — LIKE prefix seeks use it.
`prefix = query.Q.Trim()` — casing handled by CI collation (`SQL_Latin1_General_CP1_CI_AS`).

### 4.4 ListFeaturedTours

```csharp
.Where(t => t.IsFeatured && t.Status == TourStatus.Published && !t.IsDeleted)
.OrderByDescending(t => t.BookingCount).ThenByDescending(t => t.AverageRating)
.Take(20)  // SQL-level — not in-memory
```

No pagination. Hard limit 20. Cache 10 min, tag `tours:featured`.

### 4.5 ListMyTours

**IDOR prevention** — wired in endpoint, not handler:
```csharp
var isAdmin = currentUser.IsInRole("Admin") || currentUser.HasPermission("ContentTours.Tour.ReadAny");
var effectiveUserId = isAdmin && providerUserId.HasValue
    ? providerUserId.Value
    : currentUser.UserId!.Value;  // non-admins always see their own
```

**StatusFilter** — parsed to `TourStatus` enum IN-PROCESS before building the query (never `ToString()` in LINQ):
```csharp
if (!Enum.TryParse<TourStatus>(query.StatusFilter, ignoreCase: true, out var parsed))
    return Result.Invalid("Tour.InvalidStatusFilter", ...);
// then: .Where(t => t.Status == parsed)
```

**Status in projection** — `StatusByte = (byte)t.Status` in SQL, `((TourStatus)byte).ToString()` in-process.

**Cache key** includes `IncludeDeleted` to prevent role-based cache poisoning:
`ct:my-tours:{userId}:p{page}:s{pageSize}:status:{s}:sort:{sort}:del:{includeDeleted}`

`IncludeDeleted=true` only works for admins (wired in endpoint).

### 4.6 ToggleTourFeatured

```csharp
// Idempotency via domain method (raises event only on actual change)
tour.SetFeatured(cmd.IsFeatured, currentUser.UserId!.Value);
await unitOfWork.SaveChangesAsync(ct);  // IContentToursEventUnitOfWork — dispatches domain events

// Cache bust on change only:
await cache.RemoveByTagAsync("tours:featured", ct);
await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(tour.Id), ct);
await cache.RemoveByTagAsync("tours:list", ct);
await cache.RemoveByTagAsync("tours:search", ct);
await cache.RemoveByTagAsync($"my-tours:{tour.CreatedByUserId}", ct);  // provider dashboard
```

Note: uses `IContentToursEventUnitOfWork` (not plain UoW) because `Tour.SetFeatured` raises `TourFeaturedChangedDomainEvent` — the event-dispatching UoW publishes it.

---

## 5. Integration Events

All registered in `IntegrationEventTypeRegistry`. Do not add new events without registering.

| Event | Registry key | When published |
|---|---|---|
| `TourScheduleChangedIntegrationEvent` | `content-tours.schedule.changed.v1` | POST/PUT/DELETE schedule |
| `TourPricingTierChangedIntegrationEvent` | `content-tours.pricing-tier.changed.v1` | POST/PUT/DELETE/Deactivate tier |
| `TourFeaturedChangedIntegrationEvent` | `content-tours.tour.featured-changed.v1` | ToggleFeatured (via domain event handler) |

`TourEntityChangeType` enum: `Created=1, Updated=2, Deleted=3, Deactivated=4`. Never use raw strings.

For Task 2 non-aggregate handlers, write outbox via `IContentToursOutboxWriter.Enqueue(...)` before `SaveChangesAsync`. Do NOT inject `ContentToursDbContext` into Application handlers — that violates Clean Architecture and will be rejected in PR review.

---

## 6. Error Codes Reference

### Task 2A — Schedule
| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | Tour missing or soft-deleted |
| `Tour.NotOwner` | 403 | Not owner, not admin |
| `TourSchedule.NotFound` | 404 | Schedule not under this tour |
| `TourSchedule.InvalidDayOfWeek` | 400 | Byte outside 0..6 (validator catches first) |
| `TourSchedule.InvalidTimeRange` | 400 | `EndTime ≤ StartTime` (validator catches) |
| `TourSchedule.OverlapDetected` | 422 | Overlap check failed |
| `TourSchedule.AlreadyExistsWithDifferentEndTime` | 422 | Idempotency: same StartTime, different EndTime |
| `TourSchedule.DeleteBlocked` | 409 | Future bookings reference this schedule |

### Task 2B — PricingTier
| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | |
| `Tour.NotOwner` | 403 | |
| `TourPricingTier.NotFound` | 404 | Tier not under this tour |
| `TourPricingTier.NameConflict` | 409 | Case-insensitive duplicate name on same tour |
| `TourPricingTier.CurrencyMismatch` | 400 | Currency ≠ parent tour's currency |
| `TourPricingTier.InvalidPrice` | 400 | Price < 0 (validator catches) |
| `TourPricingTier.InvalidParticipantRange` | 400 | MaxParticipants ≤ MinParticipants (validator catches) |
| `TourPricingTier.AdultTierRequired` | 409 | Last active Adult tier; tour is non-Draft |

### Task 3 — Search / Featured / MyTours
| Code | HTTP | When |
|---|---|---|
| `Tour.SearchQueryRequired` | 400 | `q` empty AND no filters |
| `Tour.InvalidStatusFilter` | 400 | Unknown status string in MyTours |
| `Tour.NotFound` | 404 | Feature toggle target missing |
| `Tour.CannotFeatureNonApproved` | 409 | Feature toggle on non-Approved tour |

HTTP 422 (`Outcome.UnprocessableEntity`) is used for semantic validation failures (overlap, adult-tier, etc.) — distinct from 400 (shape) and 409 (state conflict). This is supported: `Outcome.UnprocessableEntity = 422` was added to SharedKernel.

---

## 7. Migration — What It Does

**Migration name**: `DropTierCurrencyAddParticipantTypeAndTourNameIndex`

```
UP:
  ✅ DROP COLUMN TourPricingTiers.Currency
  ✅ ADD COLUMN  TourPricingTiers.ParticipantType tinyint DEFAULT 255
  ✅ CREATE TABLE TourPricingTierTranslations (with cascade FK + unique index on TourPricingTierId+LanguageCode)
  ✅ CREATE INDEX IX_Tours_Name ON Tours.Name
  ✅ UPDATE TourPricingTiers SET ParticipantType=1 WHERE LOWER(Name) IN ('adult','adults')
       ↑ data backfill — existing "Adult" tiers get correct ParticipantType

DOWN:
  ✅ Reverses all of the above
```

**Run before any Task 2/3 endpoint testing**: `dotnet ef database update --project ContentTours.Infrastructure --startup-project YallaJo.Api --context ContentToursDbContext`

There is also an earlier migration `AddTourScheduleTimeOnlyConversionAndIndex` — run all pending migrations in order.

---

## 8. What Mahmoud Must Do (Task 1 dependencies)

These are not Mohammad's responsibility but affect whether Task 2/3 work correctly in production.

### 8.1 PW-1 — TourStatus enum migration
After Mahmoud runs PW-1, **Mohammad must update `TourStatusExtensions.cs`**:

```csharp
// ContentTours.Domain/Enums/TourStatusExtensions.cs

// Change 1: IsApproved() — used by ToggleTourFeatured
public static bool IsApproved(this TourStatus status)
    => status == TourStatus.Approved;  // was: Published

// Change 2: RequiresAdultTier() — used by AdultTierGuard
public static bool RequiresAdultTier(this TourStatus status)
    => status == TourStatus.Pending || status == TourStatus.Approved;  // was: Published

// Change 3: IsEditable() — used by AdultTierGuard
public static bool IsEditable(this TourStatus status)
    => status == TourStatus.Draft || status == TourStatus.Rejected;  // was: Draft only
```

This is the ONLY code change needed after PW-1. All handlers use the extension methods — no hunting across files.

### 8.2 Tour.Currency lock
Mahmoud's `UpdateTourCommandHandler` must add:
```csharp
var tierCount = await tierRepo.CountAsync(t => t.TourId == cmd.TourId, ct);
if (tierCount > 0 && !cmd.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
    return Result.Fail(Outcome.Conflict,
        new Error("Tour.CurrencyLockedByPricingTiers",
            "Cannot change currency while pricing tiers exist. Delete all tiers first."));
```

### 8.3 Tour.Submit gate
`Tour.Submit()` must check (in Task 1):
- `TourSchedules.Any(s => s.IsActive)` — at least one active schedule
- `TourPricingTiers.Any(t => t.IsActive && t.IsAdult)` — at least one active Adult tier

---

## 9. Known Tech Debt (logged, not blocking PR)

| # | Item | Impact | When to fix |
|---|---|---|---|
| TD-1 | `LIKE '%token%'` search is a full scan | CPU spike at > 50k tours or > 10 RPS | Before scaling — use `AddTourSearchDocument` migration |
| TD-2 | `StartTime` validator uses `NotEmpty()` — blocks `00:00` (midnight) | Blocks valid midnight schedules if business needs them | Next sprint |
| TD-3 | `SearchToursQueryHandler` — 3 separate SQL round-trips (CountAsync + facets + page) | Data may change between Q1 and Q3 | Acceptable v1 tradeoff; document in API docs |
| TD-4 | `ListTourPricingTiers` loads all tiers then all translations in 2 queries | Fine for < 50 tiers; at scale use JOIN | Next sprint |

---

## 10. File Map — Every New/Changed File

### New files (Mohammad's additions)

```
ContentTours.Domain/
  Entities/
    TourPricingTierTranslation.cs         ← new entity (Q6)
  Enums/
    ParticipantType.cs                    ← Adult=1…Other=255 (Q8)
    TourStatusExtensions.cs              ← PW-1 bridge: IsApproved/RequiresAdultTier/IsEditable (BUG-10)

ContentTours.Contracts/
  TourEntityChangeType.cs               ← replaces freeform strings in events

ContentTours.Application/
  Commands/TourSchedule/Common/
    TourScheduleOverlapChecker.cs        ← shared overlap logic (BUG-20)
  Queries/Tour/SearchTours/
    SearchToursResult.cs                 ← added FacetsAreApproximate field
  Queries/TourPricingTier/Common/
    TourPricingTierDto.cs                ← added ParticipantType (BUG-17)

ContentTours.Infrastructure/
  Migrations/
    20260426203753_DropTierCurrency...   ← schema migration (Q3/Q8/Q9 + backfill)
  Repositories/
    TourPricingTierTranslationRepository.cs
  Persistence/Configurations/
    TourPricingTierTranslationConfiguration.cs

YallaJo.SharedKernel.Domain/
  Abstractions/Results/
    Outcome.cs                           ← UnprocessableEntity = 422 added (Q2)
    Result.cs                            ← UnprocessableEntity<T> helpers added (Q2)
```

### Changed files (Mohammad's modifications)

```
ContentTours.Domain/
  Entities/TourPricingTier.cs           ← Currency computed, ParticipantType field, Translations nav

ContentTours.Contracts/
  TourScheduleChangedIntegrationEvent.cs   ← ChangeType: string → TourEntityChangeType
  TourPricingTierChangedIntegrationEvent.cs ← same; NewPrice renamed to Price

ContentTours.Application/
  Commands/TourSchedule/
    CreateTourSchedule/
      CreateTourScheduleCommand.cs       ← stripped to weekly-only (no Pattern/ValidFrom/ValidTo)
      CreateTourScheduleCommandHandler.cs ← rewritten (overlap, idempotency, 201 vs 200)
      CreateTourScheduleCommandValidator.cs ← simplified
    UpdateTourSchedule/
      UpdateTourScheduleCommandHandler.cs ← uses shared TourScheduleOverlapChecker
  Commands/TourPricingTier/
    Common/AdultTierGuard.cs             ← uses TourStatusExtensions
    CreateTourPricingTier/
      CreateTourPricingTierCommand.cs    ← ParticipantType added
      CreateTourPricingTierCommandHandler.cs ← currency from Money, ParticipantType
      CreateTourPricingTierCommandValidator.cs ← ParticipantType IsInEnum()
    UpdateTourPricingTier/
      UpdateTourPricingTierCommand.cs    ← ParticipantType added
      UpdateTourPricingTierCommandHandler.cs ← guard uses ParticipantType
      UpdateTourPricingTierCommandValidator.cs ← ParticipantType IsInEnum()
  Commands/Tour/ToggleTourFeatured/
    ToggleTourFeaturedCommandHandler.cs  ← uses IsApproved(); busts my-tours cache
  Queries/Tour/ListMyTours/
    ListMyToursQuery.cs                  ← IncludeDeleted in cache key
    ListMyToursQueryHandler.cs           ← SQL-safe filter + projection (no Enum.ToString in SQL)
  Queries/Tour/SuggestTours/
    SuggestToursQueryHandler.cs          ← EF.Functions.Like (index-friendly)
  Queries/Tour/ListFeaturedTours/
    ListFeaturedToursQueryHandler.cs     ← SQL-level Take(20)
  Queries/Tour/SearchTours/
    SearchToursQueryHandler.cs           ← exact CountAsync, token filter before facets
  Queries/TourPricingTier/ListTourPricingTiers/
    ListTourPricingTiersQueryHandler.cs  ← joins translations, maps ParticipantType
  Caching/TourSearchCacheKeys.cs        ← MyTours key includes IncludeDeleted

ContentTours.Infrastructure/
  DependencyInjection.cs                ← registers ITourPricingTierTranslationRepository
  Persistence/Configurations/
    TourPricingTierConfiguration.cs     ← Ignore(Currency), ParticipantType conversion
  EventHandlers/
    LanguageActivatedIntegrationEventHandler.cs ← extended to create TierTranslation stubs

ContentTours.Presentation/
  TourScheduleEndpoints.cs             ← simplified request DTO (no Pattern etc.)
  TourPricingTierEndpoints.cs          ← ParticipantType in request DTOs
```

### Deleted files

```
ContentTours.Application/Commands/TourSchedule/CreateTourSchedule/RecurrencePattern.cs
  ← dead code after weekly-only decision (BUG-18)
```

---

## 11. PR Checklist

Before opening the PR:

- [ ] `dotnet build YallaJo.sln` — must be green (currently ✅)
- [ ] PW-1 is merged (Mahmoud's blocker — do not open PR without it)
- [ ] Run all pending migrations: `dotnet ef database update --context ContentToursDbContext ...`
- [ ] Smoke test in Swagger:
  - POST a tour → POST schedule (Mon+Wed) → verify 201 `{ created: 2, skipped: 0 }`
  - POST same schedule again → verify 200 `{ created: 0, skipped: 2 }`
  - POST Adult tier → POST Child tier → GET pricing (verify `ParticipantType` in response)
  - Try DELETE Adult tier → verify 409 `TourPricingTier.AdultTierRequired`
  - GET `/search?q=petra` → verify `facets`, `total`, `facetsAreApproximate` in response
  - GET `/provider/my-tours` → verify pagination, status filter works
  - PATCH `/admin/{id}/feature` → verify cache bust + 200 idempotent on second call
- [ ] `SELECT TOP 10 * FROM content_tours.OutboxMessages ORDER BY OccurredOnUtc DESC` after each mutation — verify outbox rows are created
- [ ] After PW-1 is merged, update `TourStatusExtensions.cs` (3 methods, see §8.1) and re-run tests
- [ ] PR description must reference this guide and the fix plan (`MOHAMMAD_TASK2_3_FIX_PLAN.md`)

---

## 12. Quick Reference — Key Interfaces

```csharp
// Non-aggregate mutations — no event dispatch
IContentToursUnitOfWork.SaveChangesAsync(ct)

// Aggregate mutations (Tour) — dispatches domain events
IContentToursEventUnitOfWork.SaveChangesAsync(ct)

// Write integration event to outbox (Application layer — do NOT inject DbContext)
IContentToursOutboxWriter.Enqueue(IIntegrationEvent evt)

// Cross-module stub — Booking module replaces in future
IScheduleBookingCountService.GetFutureBookingCountForScheduleAsync(scheduleId, ct)
```

---

*Generated by AI review + implementation session. For questions, check `MOHAMMAD_TASK2_3_FIX_PLAN.md` for the full decision log with open questions and Tech Lead answers.*
