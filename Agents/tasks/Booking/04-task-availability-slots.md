# TASK 1 — Availability Slots (TourBooking-side)

**Owner:** Mahmoud (Intermediate)
**Endpoints:** 5
**Estimated hours:** 36
**Earliest start:** Wed 2026-06-17 (Day-2, after PW merge)
**Hard PR deadline:** Thu 2026-07-09 17:00
**Dependencies:** PW-1, PW-2, PW-3, PW-4, PW-5, PW-7, PW-8 all merged. BookingTourSnapshot table exists (PW-9 or this task's first WBS step).

---

## Endpoint list

| # | Method + Path | Permission | Returns | Errors |
|---|---|---|---|---|
| 1 | `POST /api/v1/booking/availability/slots` | `BookingFeatures.AvailabilitySlot + AppAction.Create` | 201 + `{id, tourId, date, startTime, endTime, maxCapacity, availableCount}` | 400 validation, 403 not provider of tour, 404 tour, 409 overlap |
| 2 | `POST /api/v1/booking/availability/slots/bulk` | `BookingFeatures.AvailabilitySlot + AppAction.Create` | 202 + `{createdCount, skippedDates: ["2026-07-01", …], totalRequested}` | 400, 403, 404, 422 if zero created (everything skipped) |
| 3 | `PUT /api/v1/booking/availability/slots/{id}` | `BookingFeatures.AvailabilitySlot + AppAction.Update` | 200 + same DTO as create | 400, 403, 404, 409 capacity reduction below booked |
| 4 | `DELETE /api/v1/booking/availability/slots/{id}` | `BookingFeatures.AvailabilitySlot + AppAction.Delete` | 204 | 403, 404, 409 has bookings |
| 5 | `GET /api/v1/booking/availability/{tourId}` | `AllowAnonymous` | 200 + `{items: [{date, slots: [{id, startTime, endTime, availableCount}…]}…], nextCursor, totalCount?}` | 404 tour |
| 6 | `GET /api/v1/booking/availability/{tourId}/{date}` | `AllowAnonymous` | 200 + `{slots: [...]}` | 404 tour/no slots |

> Endpoints 5 and 6 are read-side queries; they're listed under TASK 1 because Mahmoud owns the entire availability surface. Endpoint 6 is a single-date subset; can share handler with endpoint 5 via a path-param branch.

---

## Implementation notes

- **Single-slot POST contract:**
  ```json
  {
    "tourId": "<guid>",
    "date": "2026-07-15",
    "startTime": "09:00",
    "endTime": "13:00",
    "maxCapacity": 12
  }
  ```
- **Bulk POST contract (recurring 90-day pattern):**
  ```json
  {
    "tourId": "<guid>",
    "startDate": "2026-07-01",
    "endDate": "2026-09-29",
    "recurrence": "Daily" | "Weekly" | "Custom",
    "daysOfWeek": ["Mon","Wed","Fri"],
    "startTime": "09:00",
    "endTime": "13:00",
    "maxCapacity": 12,
    "skipExisting": true
  }
  ```
  - Server clamps `endDate` to `startDate + 90d`.
  - `daysOfWeek` ignored unless `recurrence == "Custom"`.
  - `skipExisting=true` (default) means existing slots at same `(tourId, date, startTime, endTime)` are silently skipped (counted in `skippedDates`).
  - If `skipExisting=false`, any collision → 409 with the offending date.
- **Capacity reduction guard (PUT):** `maxCapacity >= BookedCount + LockedCount` else `Result.Failure<T>(new Error("AvailabilitySlot.CapacityExceeded", "Cannot reduce capacity below currently booked count"), Outcome.Conflict)`.
- **Overlap detection (single + bulk):** "Overlap" = two slots for the same `(tourId, date)` whose `[startTime, endTime)` intervals intersect. Repository method: `IAvailabilitySlotRepository.AnyOverlapAsync(tourId, date, startTime, endTime, excludeId, ct)`. Use SARGable predicate: `WHERE TourId=@tid AND Date=@d AND NOT(EndTime <= @start OR StartTime >= @end) AND IsActive=1 AND (Id <> @excl OR @excl IS NULL)`.
- **DELETE blocked** when `BookedCount > 0` (regardless of LockedCount). Reason: deleting a slot with active bookings orphans them. Locks-only deletion is allowed (the lock cleanup BG will handle them).
- **GET shape:** returns dates grouped (each date has a `slots` array). Cursor = `(Date, Id)` DESC date / ASC id.
- **Provider ownership check** (single + bulk + PUT + DELETE): inject `IBookingTourSnapshotRepository`; `snapshot = await snapshots.GetAsync(tourId, ct)`. If `snapshot == null` → 404. If `snapshot.ProviderId != ICurrentUser.UserId` AND user is not admin → 403. **THIS IS A VALID `ICurrentUser` USAGE** (ownership check per INDEX §4 R2).

---

## Business Rules (B-tags map to `02-critical-rules.md`)

- **B1 — Invariants:**
  - `MaxCapacity ∈ [1, Tour.MaxGroupSize]` from snapshot.
  - `StartTime < EndTime` strictly (no zero-duration; no overnight in v1).
  - `Date >= today` in tour's TZ (for single create; bulk allowed to overlap today since `skipExisting` defaults true).
  - Non-overlapping per `(tourId, date)`.
- **B2 — Auth matrix:** see `02-critical-rules.md` §B-R11.
- **B3 — State transitions:** AvailabilitySlot has no state machine in this sprint (just `IsActive` toggle on delete = soft delete).
- **B4 — Error codes:** see `02-critical-rules.md` §B-R13.
- **B5 — Cache policy:**
  - Each `GET /availability/{tourId}` query MUST implement `ICacheableQuery`. Tag = `"availability:tour:{tourId}"`. TTL 5 min.
  - On POST/PUT/DELETE, command handler MUST `await hybridCache.RemoveByTagAsync($"availability:tour:{tourId}", ct)` AFTER SaveChanges.
  - For `GET /availability/{tourId}/{date}`, tag = `"availability:tour:{tourId}:date:{date:yyyy-MM-dd}"` (more specific so single-date queries cache separately).
- **B6 — Translation rules:** N/A (no translated fields on AvailabilitySlot).
- **B7 — Concurrency:** RowVersion mandatory on AvailabilitySlot. PUT must re-load and re-check overlap inside transaction. Bulk POST builds a `List<AvailabilitySlot>` in memory then SaveChanges in one batch; if any FK/constraint violation occurs, the whole batch rolls back.
- **B8 — Audit logging:** all 4 mutating endpoints emit `AvailabilitySlotMutationAuditEvent` (internal — not a cross-module integration event, just goes to Analytics audit log via existing audit middleware once Analytics wired). For now, `ILogger<Handler>.LogInformation("Availability slot {SlotId} {Action} by {UserId}", id, action, userId)` suffices.
- **B9 — Pagination contract:** cursor only (see §B-R10).
- **B10 — Acceptance tests:** see §6 WBS.

---

## Domain methods (added to `AvailabilitySlot.cs`)

```csharp
public static AvailabilitySlot CreateForTour(Guid tourGuideIdOrNull, Guid tourId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int maxCapacity)
{
    if (startTime >= endTime) throw new DomainInvariantException("StartTime must be before EndTime.");
    if (maxCapacity < 1) throw new DomainInvariantException("MaxCapacity must be at least 1.");

    var slot = new AvailabilitySlot
    {
        Id = Guid.CreateVersion7(),
        TourGuideId = tourGuideIdOrNull,
        TourId = tourId,
        Date = date,
        StartTime = startTime,
        EndTime = endTime,
        MaxCapacity = maxCapacity,
        BookedCount = 0,
        LockedCount = 0,
        IsActive = true,
        SlotType = SlotType.TourSlot
    };
    slot.RaiseDomainEvent(new AvailabilitySlotCapacityChangedDomainEvent(slot.Id, slot.MaxCapacity, slot.MaxCapacity));
    return slot;
}

public Result Lock(int count)
{
    if (!IsActive) return Result.Failure(new Error("AvailabilitySlot.NotActive", "Slot is not active."));
    if (count < 1) return Result.Failure(new Error("AvailabilitySlot.InvalidCount", "Count must be positive."));
    if (AvailableCount < count) return Result.Failure(new Error("AvailabilitySlot.CapacityExceeded", "Not enough capacity."));
    LockedCount += count;
    return Result.Success();
}

public Result Unlock(int count) { /* mirror; cannot go negative */ }
public Result Book(int count)   { /* moves from LockedCount → BookedCount, must already be locked */ }
public Result Cancel(int count) { /* decrements BookedCount, raises capacity event */ }

public Result UpdateCapacity(int newMax)
{
    if (newMax < BookedCount + LockedCount)
        return Result.Failure(new Error("AvailabilitySlot.CapacityExceeded", "Cannot reduce below currently held."));
    var old = MaxCapacity;
    MaxCapacity = newMax;
    MarkUpdated();
    RaiseDomainEvent(new AvailabilitySlotCapacityChangedDomainEvent(Id, old, newMax));
    return Result.Success();
}

public Result Deactivate()
{
    if (BookedCount > 0) return Result.Failure(new Error("AvailabilitySlot.HasBookings", "Cannot deactivate slot with active bookings."));
    IsActive = false;
    MarkUpdated();
    return Result.Success();
}
```

> `AvailableCount` is a computed property: `public int AvailableCount => MaxCapacity - BookedCount - LockedCount;`. EF maps it as a `[NotMapped]` getter. Configurations file does NOT include it as a column — but the read-side DTO projects it as `MaxCapacity - BookedCount - LockedCount`.

---

## Validators (FluentValidation)

```csharp
// CreateAvailabilitySlotValidator
RuleFor(x => x.TourId).NotEmpty();
RuleFor(x => x.Date).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date)).WithMessage("Date must be today or future.");
RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("StartTime must be before EndTime.");
RuleFor(x => x.MaxCapacity).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100); // hard ceiling; per-tour cap checked in handler

// CreateBulkAvailabilitySlotValidator
RuleFor(x => x.TourId).NotEmpty();
RuleFor(x => x.StartDate).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date));
RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).Must((cmd, end) => (end.DayNumber - cmd.StartDate.DayNumber) <= 90).WithMessage("Range cannot exceed 90 days.");
RuleFor(x => x.Recurrence).IsInEnum();
RuleFor(x => x.DaysOfWeek).NotEmpty().When(x => x.Recurrence == RecurrencePattern.Custom);
RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
RuleFor(x => x.MaxCapacity).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
```

---

## WBS (Work Breakdown Structure)

| Step | Sub-deliverable | Hours | Finish-by |
|---|---|---|---|
| 1 | (If PW-9 missed) Create migration `BookingAddReadSnapshots` + apply to local dev | 2 | Wed 2026-06-17 EOD |
| 2 | Refactor `AvailabilitySlot.cs` with domain methods (Lock/Unlock/Book/Cancel/UpdateCapacity/Deactivate) + raise events | 3 | Thu 2026-06-18 EOD |
| 3 | Add EF configuration (RowVersion, indexes, computed-column note) + migration `BookingAddSlotLockFilteredIndex` | 2 | Fri 2026-06-19 EOD |
| 4 | Implement `IAvailabilitySlotRepository` impl + `AnyOverlapAsync` query | 3 | Sun 2026-06-21 EOD |
| 5 | CreateAvailabilitySlot CQRS (Command + Validator + Handler + Endpoint + 3 unit tests) | 4 | Mon 2026-06-22 EOD |
| 6 | UpdateAvailabilitySlot CQRS + 3 unit tests (capacity guard) | 3 | Tue 2026-06-23 EOD |
| 7 | DeleteAvailabilitySlot CQRS + 2 unit tests | 2 | Wed 2026-06-24 EOD |
| 8 | CreateBulkAvailabilitySlots CQRS (recurrence expander + skipExisting logic) + 5 unit tests covering Daily/Weekly/Custom + skipExisting=true/false | 6 | Mon 2026-06-29 EOD |
| 9 | GetAvailabilityForTour query (cursor-paginated by date) + `ICacheableQuery` + integration test verifying cache tag | 4 | Wed 2026-07-01 EOD |
| 10 | GetAvailabilityForTourOnDate query + integration test | 2 | Thu 2026-07-02 EOD |
| 11 | DI registration in `BookingDependencyInjection.cs` (repos + handlers + validators) | 1 | Fri 2026-07-03 EOD |
| 12 | Integration test: provider creates slot → user can see it via GET; PUT updates capacity → cache invalidates; DELETE blocked when booked | 3 | Mon 2026-07-06 EOD |
| 13 | Code review + reviewer feedback cycle | 4 | Wed 2026-07-08 EOD |
| **Sum** | | **39** | |

> Buffer = 39 actual vs 36 estimate. Mahmoud should track against the lower estimate; if slipping, escalate at standup by Day-10 (Mon 2026-06-29).

---

## Edge cases to test (acceptance)

| # | Case | Expected |
|---|---|---|
| 1 | Two slots same date, intervals `[09:00, 12:00)` and `[12:00, 14:00)` | Allowed (touching) |
| 2 | Two slots same date, intervals `[09:00, 12:00)` and `[11:00, 14:00)` | 409 Overlap |
| 3 | Update slot with 8 booked, set MaxCapacity=10 | OK |
| 4 | Update slot with 8 booked, set MaxCapacity=7 | 409 CapacityExceeded |
| 5 | Delete slot with 0 bookings, 3 active locks | OK (locks orphaned, cleanup BG handles) |
| 6 | Delete slot with 1 booking | 409 HasBookings |
| 7 | Bulk Daily for 95 days | Server clamps to 90, returns `skippedDates` listing days 91-95 not created |
| 8 | Bulk Custom days=["Mon","Wed"] over 30 days | Creates ~8 slots on those weekdays only |
| 9 | Bulk skipExisting=true when 3 slots already exist | Creates remaining N-3, returns the 3 dates in `skippedDates` |
| 10 | Bulk skipExisting=false when 1 slot exists | 409 with the colliding date in body |
| 11 | POST /availability with maxCapacity=101 (tour MaxGroupSize=50) | 400 validation (101 > 100 hard ceiling) |
| 12 | POST /availability with maxCapacity=51 (tour MaxGroupSize=50) | 400 validation ("exceeds tour group size") — handler-level check |
| 13 | Non-provider tries POST | 403 |
| 14 | Anonymous calls GET | 200 |
| 15 | GET availability when 0 slots exist | 200 with empty `items` array |
| 16 | RowVersion mismatch on PUT (two PUTs race) | Second one returns 409 CapacityConflict |

---

## Files Mahmoud touches

```
Booking.Domain/Entities/AvailabilitySlot.cs                       (refactor)
Booking.Domain/Events/AvailabilitySlotCapacityChangedDomainEvent.cs (PW-3, verify exists)
Booking.Application/Commands/CreateAvailabilitySlot/...
Booking.Application/Commands/UpdateAvailabilitySlot/...
Booking.Application/Commands/DeleteAvailabilitySlot/...
Booking.Application/Commands/CreateBulkAvailabilitySlots/...
Booking.Application/Queries/GetAvailabilityForTour/...
Booking.Application/Queries/GetAvailabilityForTourOnDate/...
Booking.Application/Interfaces/IAvailabilitySlotRepository.cs     (extend with overlap query)
Booking.Infrastructure/Repositories/AvailabilitySlotRepository.cs
Booking.Infrastructure/Persistence/Configurations/AvailabilitySlotConfiguration.cs
Booking.Infrastructure/Migrations/{timestamp}_BookingAddReadSnapshots.cs  (if not in PW)
Booking.Infrastructure/Migrations/{timestamp}_BookingAddSlotLockFilteredIndex.cs
Booking.Presentation/Endpoints/AvailabilitySlotEndpoints.cs       (new sub-file)
Booking.Presentation/BookingEndpoints.cs                          (wire-up)
tests/Booking.Tests.Unit/Commands/CreateAvailabilitySlotHandlerTests.cs
tests/Booking.Tests.Unit/Commands/UpdateAvailabilitySlotHandlerTests.cs
tests/Booking.Tests.Unit/Commands/CreateBulkAvailabilitySlotsHandlerTests.cs
tests/Booking.IntegrationTests/AvailabilitySlotsRoundTripTests.cs
```
