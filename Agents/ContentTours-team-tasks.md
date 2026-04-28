# ContentTours Module — Team Task Distribution (HARD MODE)

> **Status of previous sprint**: ✅ `ContentPlaces` team sprint CLOSED (Tasks 1–8, 34 endpoints, 4 contributors). Build green, tests green, authorization hygiene backlog logged to §8 of `agent-context.md`.
>
> **This sprint**: ContentTours is the next wave. Module is **47% complete** — Domain entities, EF configurations, migrations, seeding, and `LanguageActivatedIntegrationEventHandler` are already in place. Application layer, Contracts layer, and Presentation layer are **empty**. You are building **39 endpoints** in **15 business days**.
>
> **Difficulty vs. ContentPlaces**: ⬆️ harder. +5 endpoints. Real state machine with 5 states + pre-submit validation gate. Schedule recurrence with 90-day slot generation. Full-text search + faceting + autocomplete. Cross-module `PlaceId` validation. Tour packages with atomic capacity check. Waypoint batch reorder under optimistic concurrency. One migration to rewrite the `TourStatus` enum.

---

## Sprint Window & Deadlines

| Milestone | Date & Time (AST / UTC+3) | Owner |
|---|---|---|
| 🟢 **Sprint kickoff** (all-hands, plan walkthrough) | **Mon 2026-05-04 · 09:00** | Tech Lead |
| Pre-work PR merged (enum migration + UoW fix) | Mon 2026-05-04 · 17:00 | Tech Lead + Mahmoud |
| Task 1 (Mahmoud) — Tour Core CQRS feature-complete | **Fri 2026-05-08 · 17:00** | Mahmoud |
| Task 2 (Mohammad) — Schedules + Pricing Tiers | **Thu 2026-05-14 · 17:00** | Mohammad |
| Task 3 (Mohammad) — Search / Featured / MyTours | **Mon 2026-05-18 · 17:00** | Mohammad |
| Task 4 (Ezz) — Waypoints + Guides + ChildrenInfo | **Mon 2026-05-18 · 17:00** | Ezz |
| Task 5 (Fadwa) — Tour Packages + Inclusions | **Wed 2026-05-20 · 17:00** | Fadwa |
| 🔒 **Integration freeze** (no new features, only fixes) | **Thu 2026-05-21 · 17:00** | All |
| 🏁 **Hard PR cutoff** (CI must be green) | **Fri 2026-05-22 · 17:00** | All |
| 🎉 Merge-to-main + release tag `v0.contenttours` | **Fri 2026-05-22 · 20:00** | Tech Lead |

**Daily standup**: 09:30 AST, 15 minutes, mandatory. Format: (1) what I shipped yesterday, (2) what I ship today, (3) blockers. Miss two standups → escalated to Tech Lead.

**Working days**: 15 (Sun–Thu, Fri/Sat off). Total effort budgeted: **168 person-hours** across 4 devs.

---

## Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | Est. Hours | Hard Deadline |
|---|---|---|---|---|---|
| **Mahmoud** | Intermediate | Task 1: Tour Core CQRS + Approval State Machine | 11 | 44 | Fri 2026-05-08 · 17:00 |
| **Mohammad** | Intermediate | Task 2: Schedules + PricingTiers · Task 3: Search + Featured + MyTours | 8 + 5 = 13 | 52 | Mon 2026-05-18 · 17:00 |
| **Ezz** | Beginner | Task 4: Waypoints + TourGuides + ChildrenInfo | 4 + 3 + 2 = 9 | 36 | Mon 2026-05-18 · 17:00 |
| **Fadwa** | Beginner | Task 5: TourPackages + PackageInclusions | 6 | 36 | Wed 2026-05-20 · 17:00 |
| **TOTAL** | — | 5 tasks | **39 endpoints** | **168 hrs** | Fri 2026-05-22 · 17:00 |

---

## 🗂️ Entity Ownership Matrix (master reference)

Every entity in `ContentTours.Domain/Entities/` is assigned to exactly ONE owner. Use this table to know who writes a given file and who ONLY reads it.

| # | Entity File | Base Class | Aggregate? | Raises Domain Events? | **Owner** | Task |
|---|---|---|---|---|---|---|
| 1 | `Tour.cs` | `AuditableEntity` | ✅ `IAggregateRoot` | ✅ **8 events** (Created, Updated, Submitted, Approved, Rejected, Suspended, Reinstated, FeaturedChanged) | **Mahmoud** (primary) · Ezz adds `UpdateChildrenInfo` method · Mohammad adds `SetFeatured` method (if not by Mahmoud) | 1, 3, 4C |
| 2 | `TourTranslation.cs` | `BaseEntity` | ❌ | ❌ | **Mahmoud** (read-only; populated by `IEntityTranslationOrchestrator` from the Created/Updated event handlers) | 1 |
| 3 | `TourSchedule.cs` | `BaseEntity` | ❌ non-aggregate | ❌ direct outbox write only | **Mohammad** | 2A |
| 4 | `TourPricingTier.cs` | `BaseEntity` | ❌ non-aggregate | ❌ direct outbox write only | **Mohammad** | 2B |
| 5 | `TourWaypoint.cs` | `BaseEntity` | ❌ non-aggregate | ❌ direct outbox write only (optional, skipped this sprint) | **Ezz** | 4A |
| 6 | `TourTourGuide.cs` | **Junction** (composite PK, no base class) | ❌ non-aggregate | ❌ direct outbox write only | **Ezz** | 4B |
| 7 | `TourPackage.cs` | `AuditableEntity` → possibly `IAggregateRoot` (WBS 5.1 decision) | ⚠️ **TBD** | ⚠️ **TBD** (2 events if Option A: Created, Updated; else direct outbox writes) | **Fadwa** | 5 |
| 8 | `TourPackageInclusion.cs` | `BaseEntity` | ❌ non-aggregate | ❌ no events (child of Package) | **Fadwa** | 5 |

### New files Task 4 creates in `ContentTours.Domain/Enums/`

| Enum | Purpose | Owner |
|---|---|---|
| `WaypointType` (byte) | Replaces raw byte field on `TourWaypoint` — `Start / Stop / Meal / Photo / Landmark / RestStop / End` | Ezz |
| `ChildFacility` (byte) | Controlled vocabulary for the CSV `Tour.ChildFacilities` column | Ezz |

### Schema migrations driven by this sprint

| Migration | Driven by | Task | What changes |
|---|---|---|---|
| `UpdateTourStatusEnum` | Mahmoud | PW-1 (blocker, Mon Wk1) | Rewrites `TourStatus` enum from `{Draft, Published, Archived, Suspended}` → `{Draft, Pending, Approved, Rejected, Suspended, Archived}` |
| `AddTourApprovalAuditFields` | Mahmoud | 1 (piggy-backs on PW-1) | Adds 9 audit columns to `Tours` table: `SubmittedAt`, `ApprovedAt`, `ApprovedByUserId`, `RejectedAt`, `RejectedByUserId`, `RejectionReason`, `SuspendedAt`, `SuspensionReason`, `ReinstatedAt` |
| `AddTourChildrenInfoFields` | Ezz | 4C | Adds 3 nullable columns to `Tours` table: `MinChildAge int?`, `MaxChildAge int?`, `ChildFacilities nvarchar(500)?`. Also converts `TourWaypoint.WaypointType` storage to enum-backed byte |
| `AddTourSearchDocument` (conditional) | Mohammad | 3 (R-3 mitigation) | Adds persisted computed column `SearchDocument` on `Tours` table if full-text performance is poor |
| `AddTourPackageAudit` | Fadwa | 5 | Adds `CreatedByUserId Guid` to `TourPackages` table (for ownership tracking) + possibly marks `TourPackage` as `IAggregateRoot` (no schema change but EF config update) |
| `AddTourTourGuideAuditFields` (optional, deferred) | Ezz | 4B (may skip) | Would add `CreatedAt` to `TourTourGuides` junction; this sprint uses deterministic "smallest-id" rule instead to avoid schema churn |

### New interfaces / abstractions created this sprint

| Interface | Location | Stub shipped this sprint | Real implementation owner | Consumers in this sprint |
|---|---|---|---|---|
| `IPlaceExistsService` | `ContentTours.Application/Interfaces/` | In-module EF cross-DbContext query | Future SharedKernel abstraction | Task 1 (Create/Update validation, Submit gate) |
| `IAttachmentQueryService` | `ContentCore.Contracts` (may already exist; confirm) | None needed if exists | ContentCore | Task 1 Submit gate (`CountByEntityAsync`) |
| `IScheduleBookingCountService` | `ContentTours.Application/Interfaces/` | `NoOpScheduleBookingCountService` returns 0 | Booking module (future) | Task 2A delete-blocker |
| `ITourCapacityService` | `ContentTours.Application/Interfaces/` | `NoOpTourCapacityService` returns `AllHaveCapacity=true` | Booking module (future) | Task 5 (CreatePackage, AddInclusion) |
| `IUserRoleChecker` | `ContentTours.Application/Interfaces/` | Stub returns `true` + TODO log | Security module (future) | Task 4B (AssignTourGuide role validation) |
| `IProfileLookupService` | `ContentTours.Application/Interfaces/` | Fallback: `displayName = userId.ToString()` | Accounts module (future) | Task 4B (public `ListTourGuides` response) |

---

## Pre-Work (BLOCKER — must land before any feature work starts)

These two fixes are on the critical path. **Mahmoud drives, Tech Lead reviews same-day.** Target: PR merged by **Mon 2026-05-04 · 17:00**.

### PW-1: Rewrite `TourStatus` enum + migration

Current enum in `ContentTours.Domain/Enums/TourStatus.cs`:
```csharp
public enum TourStatus : byte { Draft = 0, Published = 1, Archived = 2, Suspended = 3 }
```

Required enum (matches Phase 1 spec & business rules PDF §ContentTours):
```csharp
public enum TourStatus : byte
{
    Draft = 0,
    Pending = 1,      // submitted for approval
    Approved = 2,
    Rejected = 3,
    Suspended = 4,
    Archived = 5
}
```

Migration: `UpdateTourStatusEnum`. Seeds currently hardcode `Status = 0` (Draft) so no data rewrite needed, but CI must prove the seed still loads. Commands:
```powershell
dotnet ef migrations add UpdateTourStatusEnum `
  --project ContentTours.Infrastructure `
  --startup-project YallaJo.Api `
  --context ContentToursDbContext
```

### PW-2: `IContentToursUnitOfWork` does NOT dispatch domain events (same bug class as ContentPlaces ERR-ContentPlaces-fixes §Fix 3)

Current implementation in `ContentTours.Infrastructure/Persistence/ContentToursUnitOfWork.cs` just wraps `context.SaveChangesAsync()`. Raising `TourCreatedDomainEvent` through this path will **silently drop the event** — no outbox row, no integration event, no translation trigger.

**Policy for this sprint** (applies to every handler you write):
- Command handlers mutating an **aggregate root** (`Tour`, `TourPackage`) MUST inject `IUnitOfWork<ContentToursDbContext>` (SharedKernel). This is the one that dispatches events.
- Command handlers mutating **non-aggregates only** (`TourSchedule`, `TourWaypoint`, `TourPricingTier`, `TourPackageInclusion`, `TourTourGuide`) MAY inject `IContentToursUnitOfWork`. Those entities raise no events.
- If you must publish an integration event from a non-aggregate handler, write the `OutboxMessage` **directly via `ContentToursDbContext.OutboxMessages.Add(...)`** before `SaveChangesAsync`. Never call `IPublisher.Publish` for cross-module events.

Mahmoud lands a 1-line DI registration confirming both UoWs are wired and ships 1 sanity test that fires a `TourCreatedDomainEvent` and asserts a matching `OutboxMessage` row exists post-save.

---

## Critical Rules for ALL Tasks (non-negotiable — violation = PR rejected)

Read `Agents/agent-context.md` §0.3 (Five Non-Negotiable Rules) before writing any code. The list below is additive for this sprint.

1. **Authorization**: every endpoint has `.WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.X, AppAction.Y))` OR `.AllowAnonymous()`. No bare `.RequireAuthorization()`. No string-based `.RequireAuthorization("Permission.Tour.Create")`.
2. **`ICurrentUser` discipline** (`agent-context.md` §2.2): inject **only** to compare user ID against a resource field (ownership / IDOR / self-edit / creator stamp). Do NOT inject to check `IsAuthenticated` — that's the endpoint's `MustHavePermission`'s job.
3. **`Guid.CreateVersion7()`** in every factory method. Never `Guid.NewGuid()`.
4. **`DateTime.UtcNow`** everywhere. Never `DateTime.Now`.
5. **`CancellationToken ct`** on every async method, forwarded to every `.ToListAsync(ct)`, `.FirstOrDefaultAsync(ct)`, `.AnyAsync(ct)`, `.SaveChangesAsync(ct)`, outbound HTTP calls.
6. **`ILogger<THandler>`** injected in every handler — commands AND queries. No exceptions (ERR-011 cost us 21 retrofits).
7. **Result pattern**: business errors → `Result.Failure(new Error("Tour.X", "msg"), Outcome.Y)`. Never throw for user-facing errors. Never use the `.NotFound(string)` / `.Conflict(string)` overloads — they populate `Messages`, not `Errors`, and get stripped by `ToApiResult()` (ERR-004).
8. **Return DTOs**, never entity objects. List endpoints → `{Entity}SummaryDto` (5–8 fields). Detail endpoints → `{Entity}DetailDto`.
9. **Caching**: every query implements `ICacheableQuery` with fine-grained tags. Every command handler injects `HybridCache` and calls `RemoveByTagAsync` on the most-specific tag after a successful `SaveChangesAsync` (ERR-010).
10. **State-change guards**: before calling any domain method that raises a domain event, check `if (entity.Status != targetStatus)` — unconditional calls produce duplicate outbox rows (ERR-009).
11. **Try/catch policy** (`Agents/patterns/error-handling-patterns.md`): every command handler wraps `SaveChangesAsync` in `DbUpdateConcurrencyException` catch; every handler has outer `OperationCanceledException when ct.IsCancellationRequested` catch. No try/catch in endpoints.
12. **Error codes** follow `{Entity}.{Reason}` (`Tour.NotFound`, `Tour.InvalidTransition`, `TourSchedule.OverlapDetected`, `TourPricingTier.MissingAdultTier`, `TourPackage.AtomicBookingFailed`).
13. **`dotnet build YallaJo.sln`** must be 0 errors and `dotnet test` green **on your branch** before you open a PR. Integration freeze enforces this.

---

## Task 1 — Mahmoud · Tour Core CQRS + Approval State Machine

**11 endpoints · 44 hours · Deadline: Fri 2026-05-08 · 17:00**
**No dependencies. START AT KICKOFF. Unblocks Tasks 2/3/4/5.**

### 🎯 Entities Touched

| Entity | Base Class | Role | Mahmoud's Responsibility |
|---|---|---|---|
| **`Tour`** | `AuditableEntity, IAggregateRoot` | **Primary aggregate** of this module | Own the full lifecycle: add all domain methods (`Create`, `Update`, `SoftDelete`, `Submit`, `Approve`, `Reject`, `Suspend`, `Reinstate`, `SetFeatured`, `UpdateRating`, `UpdateBookingCount`, `ApplyDiscount`, `RemoveDiscount`). Owns 7 domain events (Created, Updated, Submitted, Approved, Rejected, Suspended, Reinstated). Schema migration adds: `SubmittedAt`, `ApprovedAt`, `ApprovedByUserId`, `RejectedAt`, `RejectedByUserId`, `RejectionReason`, `SuspendedAt`, `SuspensionReason`, `ReinstatedAt`. |
| `TourTranslation` | `BaseEntity` | Child of `Tour` (already scaffolded, has factory) | Read-only from Mahmoud's perspective: the `TourCreatedDomainEventHandler` invokes `IEntityTranslationOrchestrator` which populates `TourTranslation` rows — Mahmoud does NOT write to this entity directly, but DOES read it in `GetTourById` / `GetTourBySlug` / `ListTours` for `Accept-Language` resolution via `COALESCE(tt.Name, tour.Name)`. |

> **Key rule**: `Tour` is the ONLY aggregate root in Task 1. All domain events flow through it. Use `IUnitOfWork<ContentToursDbContext>` to dispatch them — NOT `IContentToursUnitOfWork` (see PW-2).

### Endpoints

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 1 | GET | `/api/v1/tours` | Anonymous | ListTours |
| 2 | GET | `/api/v1/tours/{id:guid}` | Anonymous | GetTourById |
| 3 | GET | `/api/v1/tours/slug/{slug}` | Anonymous | GetTourBySlug |
| 4 | POST | `/api/v1/tours` | `Tour.Create` | CreateTour |
| 5 | PUT | `/api/v1/tours/{id:guid}` | `Tour.Update` | UpdateTour |
| 6 | DELETE | `/api/v1/tours/{id:guid}` | `Tour.Delete` | DeleteTour (soft) |
| 7 | POST | `/api/v1/tours/{id:guid}/submit` | `Tour.Submit` | SubmitTour (Draft→Pending, GATED) |
| 8 | POST | `/api/v1/tours/admin/{id:guid}/approve` | `Tour.Approve` | ApproveTour (Pending→Approved) |
| 9 | POST | `/api/v1/tours/admin/{id:guid}/reject` | `Tour.Reject` | RejectTour (Pending→Rejected, reason required) |
| 10 | POST | `/api/v1/tours/admin/{id:guid}/suspend` | `Tour.Suspend` | SuspendTour (Approved→Suspended, reason required) |
| 11 | POST | `/api/v1/tours/admin/{id:guid}/reinstate` | `Tour.Reinstate` | ReinstateTour (Suspended→Approved) |

### Tour Approval State Machine (get this right — everything downstream depends on it)

```
        ┌────────────┐
        │   Draft    │ ◄──────┐   (provider can edit freely)
        └──────┬─────┘        │
               │ Submit        │
               │ (GATE)        │ Reject (with reason)
               ▼               │
        ┌────────────┐         │
        │  Pending   │─────────┘
        └──────┬─────┘
               │ Approve
               ▼
        ┌────────────┐◄──────────┐
        │  Approved  │           │ Reinstate
        └──────┬─────┘           │
               │ Suspend         │
               │ (with reason)   │
               ▼                 │
        ┌────────────┐           │
        │ Suspended  │───────────┘
        └────────────┘
        
   (Rejected → Draft on next edit by owner — user re-edits & re-submits)
   (Archived: out of scope this sprint — add enum value, no endpoint yet)
```

Every state transition guards the current status. If the precondition doesn't match, return:
```csharp
return Result.Failure(
    new Error("Tour.InvalidTransition",
        $"Cannot {action} a tour with status {currentStatus}."),
    Outcome.Conflict);
```

### Pre-Submit Validation Gate (the HARD part of this task)

When `SubmitTour` is called, run **all** of these checks BEFORE transitioning to `Pending`. Return all failures in a single response (aggregate them):

| Check | Error Code | Failure Reason |
|---|---|---|
| Tour exists, not soft-deleted, owner matches current user | `Tour.NotFound` / `Tour.NotOwner` | 404 / 403 |
| Tour status is `Draft` (not `Pending`/`Approved`/`Rejected`) | `Tour.InvalidTransition` | 409 |
| At least 1 `Attachment` of type `Image` linked to this tour via ContentCore (call `IAttachmentQueryService.CountByEntityAsync(EntityType.Tour, tourId, ct)`) | `Tour.NoImages` | 422 |
| At least 1 active `TourPricingTier` exists for this tour | `Tour.NoPricing` | 422 |
| At least 1 active `TourSchedule` exists for this tour | `Tour.NoSchedule` | 422 |
| `Description != null && Description.Length >= 100` | `Tour.DescriptionTooShort` | 422 |
| `MeetingPoint != null` (both lat & lng set) | `Tour.MissingMeetingPoint` | 422 |
| `PlaceId` still references a non-deleted place (cross-module — use cached `IPlaceExistsService` or direct DB query through SharedKernel) | `Tour.PlaceDeleted` | 422 |

Response shape on failure:
```json
{ "status": 422, "title": "Tour.SubmitValidationFailed",
  "errors": [
    { "code": "Tour.NoImages",          "description": "Upload at least one image before submitting." },
    { "code": "Tour.DescriptionTooShort", "description": "Description must be at least 100 characters." }
  ] }
```

### Business Rules

#### B1. Domain Invariants (always true on a `Tour` aggregate)

1. **Ownership is immutable**: `CreatedByUserId` is set once in `Create()` and NEVER changes. It's the only reference the ownership gate uses. Transferring tours between providers is out of scope.
2. **Slug is globally unique across all non-deleted tours**: enforced at DB level (unique filtered index `WHERE IsDeleted = 0`) AND in the handler (`ExistsBySlugAsync` — because DB constraint only fires at save-time). Even soft-deleted tours keep their slug reserved for 30 days (grace window for accidental deletes); after 30 days it becomes reusable.
3. **Currency is a 3-letter ISO 4217 code**: stored `ToUpperInvariant()`. Whitelist for Phase 1: `JOD`, `USD`, `EUR`. Anything else → `Tour.UnsupportedCurrency`. All monetary fields on this Tour (`BasePrice`, `SalePrice`, every child `TourPricingTier.Price`, every child `TourPackage.Price`) use this same currency — see Task 2 `TourPricingTier.CurrencyMismatch`.
4. **Meeting point is inside Jordan bounding box (soft check)**: if Lat/Lng set, warn (not block) if outside `[29.0..33.5] × [34.8..39.4]` via log — Tech Lead may tighten later. Hard reject only on math-invalid coords.
5. **Rating & BookingCount are denormalized**: only updated by `UpdateRating(decimal avg, int reviewCount)` and `UpdateBookingCount(int delta)` methods — no public setter, no direct DB update. Social module writes Rating via integration event; Booking writes BookingCount via `TourBookingCountChangedIntegrationEvent` (future).
6. **Discount fields are coherent**: if any of (`DiscountPercent`, `SalePrice`, `DiscountValidFrom`, `DiscountValidTo`) is set, the full set must be consistent: `DiscountValidFrom < DiscountValidTo`, `SalePrice < BasePrice`, `DiscountPercent ≈ (1 - SalePrice/BasePrice) * 100` (within 0.5% tolerance). Domain method `ApplyDiscount(decimal percent, DateTime from, DateTime to)` computes `SalePrice` from `BasePrice` — never trust the client. `RemoveDiscount()` clears all four fields atomically. (Finance module calls these later; implement domain methods now.)
7. **Status transitions only through domain methods**: `Submit()`, `Approve()`, `Reject(reason)`, `Suspend(reason)`, `Reinstate()`. Each method guards the current status — illegal transitions throw `InvalidOperationException` (domain invariant, not business error). The handler guards the status check BEFORE calling the method and returns `Result.Failure(...Tour.InvalidTransition...)`; the domain throw is a safety net against handler bugs.
8. **Soft-delete is terminal**: `IsDeleted = true` cannot be reversed. To "undelete", admin creates a new tour. No un-delete endpoint.

#### B2. Authorization Matrix

| Endpoint | Anonymous | Authenticated | Owner (CreatedByUserId) | Admin (`Admin` role) | Required Permission |
|---|---|---|---|---|---|
| `GET /tours` | ✅ Approved-only | ✅ Approved-only | ✅ Approved-only | ✅ all statuses via filter | none (list is public) |
| `GET /tours/{id}` | ✅ Approved-only | ✅ Approved-only | ✅ any status (own) | ✅ any status | none |
| `GET /tours/slug/{slug}` | ✅ Approved-only | ✅ Approved-only | ✅ any status (own) | ✅ any status | none |
| `POST /tours` | ❌ | ✅ | ✅ creator = self | ✅ can stamp `CreatedByUserId` via body | `ContentTours.Tour.Create` |
| `PUT /tours/{id}` | ❌ | ✅ (if owner) | ✅ | ✅ any | `ContentTours.Tour.Update` |
| `DELETE /tours/{id}` | ❌ | ✅ (if owner) | ✅ (if no bookings) | ✅ | `ContentTours.Tour.Delete` |
| `POST /tours/{id}/submit` | ❌ | ✅ (if owner) | ✅ | ✅ | `ContentTours.Tour.Submit` |
| `POST /tours/admin/{id}/approve` | ❌ | ❌ | ❌ | ✅ | `ContentTours.Tour.Approve` |
| `POST /tours/admin/{id}/reject` | ❌ | ❌ | ❌ | ✅ | `ContentTours.Tour.Reject` |
| `POST /tours/admin/{id}/suspend` | ❌ | ❌ | ❌ | ✅ | `ContentTours.Tour.Suspend` |
| `POST /tours/admin/{id}/reinstate` | ❌ | ❌ | ❌ | ✅ | `ContentTours.Tour.Reinstate` |

**IDOR rule** (handler-level, per `agent-context.md` §2.2): inject `ICurrentUser` in `UpdateTour`, `DeleteTour`, `SubmitTour` handlers ONLY. Check `tour.CreatedByUserId == currentUser.UserId.Value || currentUser.IsInRole("Admin")` → `Result.Failure(Error.Forbidden("Tour.NotOwner"), Outcome.Forbidden)` on mismatch. Do NOT inject `ICurrentUser` in any query handler or in admin-only command handlers — the endpoint's `MustHavePermission` gate is sufficient.

#### B3. State Transition Table (precondition → transition → postcondition)

| Transition | Endpoint | Preconditions | Postcondition | Domain Event Raised | Side Effects (in event handler, same TX) |
|---|---|---|---|---|---|
| Create | `POST /tours` | payload valid, slug unique, `PlaceId` exists if set | `Status = Draft`, `IsActive = true`, `IsFeatured = false`, `AverageRating = 0`, `ReviewCount = 0`, `BookingCount = 0` | `TourCreatedDomainEvent` | Trigger `EntityTranslationOrchestrator` for (Ar, En). Write `TourCreatedIntegrationEvent` to outbox. |
| Update | `PUT /tours/{id}` | exists, not deleted, current status ∈ {Draft, Rejected}, slug unique-excluding-self, `PlaceId` valid, rowVersion matches | fields updated, `Status` auto-resets to `Draft` if was `Rejected` (clears `RejectionReason`) | `TourUpdatedDomainEvent` | Re-trigger translation IF `Name`/`Description`/`ShortDescription` changed. Write `TourUpdatedIntegrationEvent` to outbox. |
| SoftDelete | `DELETE /tours/{id}` | exists, not already deleted, `BookingCount == 0`, no future schedules if status == Approved | `IsDeleted = true`, `DeletedAt = UtcNow` | — (no domain event; handler writes integration event directly) | Write `TourDeletedIntegrationEvent` to outbox. Cache invalidate `tour:{id}`, `tours:list`, `tours:featured`. |
| Submit | `POST /tours/{id}/submit` | status == Draft, ALL 8 gate checks pass | `Status = Pending`, `SubmittedAt = UtcNow` | `TourSubmittedDomainEvent` | Write `TourSubmittedIntegrationEvent` to outbox (Messaging picks up → notify admin queue). |
| Approve | `POST /tours/admin/{id}/approve` | status == Pending | `Status = Approved`, `ApprovedAt = UtcNow`, `ApprovedByUserId = currentUser.UserId`, `RejectionReason = null` | `TourApprovedDomainEvent` | Write `TourApprovedIntegrationEvent`. ContentSeo indexes. Messaging notifies provider. |
| Reject | `POST /tours/admin/{id}/reject` | status == Pending, `Reason` non-empty | `Status = Rejected`, `RejectionReason = Reason`, `RejectedAt = UtcNow`, `RejectedByUserId = currentUser.UserId` | `TourRejectedDomainEvent` | Write `TourRejectedIntegrationEvent`. Messaging notifies provider with reason. |
| Suspend | `POST /tours/admin/{id}/suspend` | status == Approved, `Reason` non-empty | `Status = Suspended`, `SuspensionReason = Reason`, `SuspendedAt = UtcNow` | `TourSuspendedDomainEvent` | Write `TourSuspendedIntegrationEvent`. Messaging notifies provider. Booking blocks new bookings. ContentSeo de-indexes. |
| Reinstate | `POST /tours/admin/{id}/reinstate` | status == Suspended | `Status = Approved`, `SuspensionReason = null`, `ReinstatedAt = UtcNow` | `TourReinstatedDomainEvent` | Write `TourReinstatedIntegrationEvent`. Messaging notifies provider. ContentSeo re-indexes. |

**NEW fields required on `Tour`** (schema migration `AddTourApprovalAuditFields` — piggy-back onto PW-1 migration): `SubmittedAt DateTime?`, `ApprovedAt DateTime?`, `ApprovedByUserId Guid?`, `RejectedAt DateTime?`, `RejectedByUserId Guid?`, `RejectionReason nvarchar(1000)?`, `SuspendedAt DateTime?`, `SuspensionReason nvarchar(1000)?`, `ReinstatedAt DateTime?`.

#### B4. Error Code Catalog (Task 1)

| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | `GetTourById`, `GetTourBySlug`, update/delete/submit/approve/etc. target missing or soft-deleted (and caller is not admin) |
| `Tour.NotOwner` | 403 | IDOR — caller is neither `CreatedByUserId` nor admin |
| `Tour.SlugConflict` | 409 | Slug already used by another non-deleted tour on Create / Update |
| `Tour.UnsupportedCurrency` | 400 | Currency not in `{JOD, USD, EUR}` |
| `Tour.PlaceDeleted` | 422 | `PlaceId` references soft-deleted place |
| `Tour.PlaceNotFound` | 422 | `PlaceId` doesn't exist at all |
| `Tour.InvalidCoordinates` | 400 | Location lat/lng outside valid range OR 0/0 |
| `Tour.InvalidTransition` | 409 | State machine precondition violated (see B3) |
| `Tour.ConcurrencyConflict` | 409 | `DbUpdateConcurrencyException` on save — caller's `rowVersion` stale |
| `Tour.DeleteBlocked` | 409 | Delete attempted with `BookingCount > 0` or future schedules exist on Approved tour |
| `Tour.DiscountInconsistent` | 400 | Discount fields violate B1.6 coherence rule |
| `Tour.ReasonRequired` | 400 | Reject / Suspend called with empty `Reason` |
| `Tour.NoImages` | 422 | SubmitTour gate: no attachments |
| `Tour.NoPricing` | 422 | SubmitTour gate: no active pricing tier |
| `Tour.NoSchedule` | 422 | SubmitTour gate: no active schedule |
| `Tour.DescriptionTooShort` | 422 | SubmitTour gate: description <100 chars |
| `Tour.MissingMeetingPoint` | 422 | SubmitTour gate: `MeetingPoint` is null |
| `Tour.NoAdultPricingTier` | 422 | SubmitTour gate: no tier named `"Adult"` exists (ties in with Task 2) |
| `Tour.SubmitValidationFailed` | 422 | Umbrella code — response aggregates the individual 422 errors above |

#### B5. Cache Policy

| Query | Cache Key | Absolute TTL | Local L1 | Tags |
|---|---|---|---|---|
| `ListTours` | `ct:tours:p{page}:s{pageSize}:lang:{Accept-Language}:cat:{categoryId}:place:{placeId}:…(all filter values)` | 5 min | 2 min | `tours`, `tours:list` |
| `GetTourById` | `ct:tour:{id}:lang:{Accept-Language}` | 5 min | 2 min | `tours`, `tour:{id}` |
| `GetTourBySlug` | `ct:tour:slug:{slug}:lang:{Accept-Language}` | 5 min | 2 min | `tours`, `tour:{id}` (resolve id first; cache under both keys) |

**Command invalidation matrix**:

| Command | Tags invalidated (after successful `SaveChangesAsync`) |
|---|---|
| CreateTour | `tours:list`, `tours:search` (Task 3) |
| UpdateTour | `tour:{id}`, `tours:list`, `tours:search`, `tour-schedules:{id}` (if pre-submit gate ran), `tour-pricing:{id}` |
| DeleteTour | `tour:{id}`, `tours:list`, `tours:featured`, `tours:search` |
| SubmitTour | `tour:{id}`, `tours:list` |
| ApproveTour / RejectTour / SuspendTour / ReinstateTour | `tour:{id}`, `tours:list`, `tours:featured`, `tours:search` |

**Rule**: never use a coarse `tours` tag in a single-tour mutation — that evicts every list cache entry system-wide (ERR-010). Use `tour:{id}` + `tours:list` instead.

#### B6. Translation Handling

- Primary language stored inline on `Tour` (`Name`, `Description`, `ShortDescription`, `MetaTitle`, `MetaDescription`). Additional languages live in `TourTranslation` rows (already scaffolded, has factory method).
- On **Create**: `TourCreatedDomainEventHandler` invokes `IEntityTranslationOrchestrator.TranslateEntityAsync(entityType: "Tour", entityId, sourceLanguage: tour.Currency == "JOD" ? "ar" : "en", targetLanguages: activeLanguages.Where(l => l.Code != source), ct)`. Orchestrator produces `TourTranslation` rows; handler attaches them via `dbContext.TourTranslations.Add(...)` — UoW commits everything atomically.
- On **Update**: handler diffs old vs new on name/description/short-description. If any changed → re-trigger orchestrator (which does a targeted refresh, not a full rebuild).
- Translation is **non-blocking**: if `EntityTranslationOrchestrator` fails (Azure quota, HTTP timeout), log at `Warning` level and let the `TourTranslationBackfillBackgroundService` (future) retry. Do NOT fail the Create/Update command because a translation call failed.
- `Accept-Language` on GET determines which translation wins: SQL `COALESCE(tt.Name, tour.Name)` pattern, filtered by `tt.LanguageCode == requested`.

#### B7. Concurrency Handling

- Every write command loads the aggregate with tracking (`asNoTracking: false`). RowVersion comes back in the DTO on GET and is expected in the body on PUT (`[FromBody] UpdateTourRequest { ..., byte[] RowVersion }`). EF's concurrency token handles the rest — a stale RowVersion → `DbUpdateConcurrencyException` → caught in handler → `Result.Failure(Error.Conflict("Tour.ConcurrencyConflict", "…"), Outcome.Conflict)`.
- Never use `DbUpdateConcurrencyException` from `Microsoft.EntityFrameworkCore` in Application handlers — ContentTours doesn't yet have a custom exception wrapper like ContentPlaces (see ERR-006). **Action item for Mahmoud**: in PW-2, decide whether to add `ContentToursConcurrencyException` wrapper in Domain.Exceptions. If NOT added, catch `DbUpdateConcurrencyException` directly AND add `using Microsoft.EntityFrameworkCore;` to the handler file. Document the decision in PR description.
- State-machine commands (Approve/Reject/Suspend/Reinstate) must call `tour.RowVersion` match check BEFORE the domain method — this prevents a second admin from overwriting a concurrent approval even if the DB RowVersion check passes later.

#### B8. Audit Logging Requirements

- Every command handler logs at `Information` level:
  - Entry: `logger.LogInformation("Tour {Action} invoked by {UserId} for TourId={TourId}", nameof(ApproveTour), currentUser.UserId, cmd.Id);`
  - Success: `logger.LogInformation("Tour {Action} succeeded: TourId={TourId} NewStatus={Status}", nameof(ApproveTour), tour.Id, tour.Status);`
  - Business failure: `logger.LogInformation("Tour {Action} rejected: {ErrorCode} TourId={TourId}", nameof(ApproveTour), error.Code, cmd.Id);` — NOT `Warning` or `Error`, it's not a bug.
- Concurrency exception logs at `Warning` level with the stale RowVersion.
- Never log the `RejectionReason` or `SuspensionReason` at Information level — they may contain PII. Use `Debug` for reason content.

#### B9. Pagination & Filter Semantics (list endpoint)

- `page` default 1, min 1. `pageSize` default 20, max 50. Validator rejects out-of-range.
- Empty `q` is valid (list without search). Empty filter set is valid (returns all approved tours).
- Sort values (`sort` query param): `price_asc`, `price_desc`, `rating_desc`, `popularity_desc` (by `BookingCount DESC`), `newest` (by `CreatedAt DESC`). Default: `newest` when no `q`, `relevance` when `q` is set (Task 3).
- Response shape: `PaginatedResult<TourSummaryDto>` = `{ items: […], total: int, page: int, pageSize: int, totalPages: int }`.
- `total` is a separate `CountAsync(ct)` query — expensive; cache it with the same key as the list.

#### B10. Acceptance Test Scenarios (write these as unit/integration tests)

1. Create tour → GET by id returns same data with `Status = Draft`.
2. Create tour with duplicate slug → `Tour.SlugConflict` 409.
3. Create tour with `PlaceId` of a deleted place → `Tour.PlaceDeleted` 422.
4. Update tour as non-owner non-admin → `Tour.NotOwner` 403.
5. Submit a Draft tour missing pricing → 422 with `{ errors: [ Tour.NoPricing, Tour.NoSchedule, … ] }` (aggregate).
6. Submit a valid Draft → transitions to Pending, writes `TourSubmittedIntegrationEvent` to outbox.
7. Admin approves a Pending tour → transitions to Approved, emits integration event.
8. Reject without reason → `Tour.ReasonRequired` 400.
9. Reject a Pending tour → transitions to Rejected with reason stored.
10. Update a Rejected tour → auto-transitions to Draft, `RejectionReason` cleared, `TourUpdatedDomainEvent` raised.
11. Delete tour with `BookingCount > 0` → `Tour.DeleteBlocked` 409.
12. Suspend an Approved tour → transitions to Suspended, ContentSeo and Booking integration events fire.
13. Reinstate a Suspended tour → transitions to Approved, integration events fire.
14. Two admins approve concurrently → first succeeds, second gets `Tour.ConcurrencyConflict` 409.
15. Create tour → on new-language activation (Ar→En or En→Ar), existing translation backfill runs via `LanguageActivatedIntegrationEventHandler` (already shipped).

### Domain & Integration Event Handler Logic

#### `TourCreatedDomainEventHandler` (lives in `ContentTours.Application/EventHandlers/` per §3.3 convention, but MAY live in `ContentTours.Infrastructure/EventHandlers/` if it needs `ContentToursDbContext` to write outbox rows — this codebase does the latter; see ContentPlaces-fixes-required.md §Handler Placement Rule)

```csharp
public sealed class TourCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ContentToursDbContext dbContext,
    IOptions<ContentToursOptions> options,
    ILogger<TourCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourCreatedDomainEvent> note, CancellationToken ct)
    {
        var evt = note.DomainEvent;

        // 1. Trigger multi-language translation (non-blocking).
        //    Orchestrator writes TourTranslation rows to the SAME DbContext.
        //    UoW commits them atomically with the Tour insert.
        try
        {
            await orchestrator.TranslateEntityAsync(
                entityType: "Tour",
                entityId: evt.TourId,
                fields: new[] { "Name", "Slug", "Description", "ShortDescription", "MetaTitle", "MetaDescription" },
                sourceLanguageCode: options.Value.DefaultSourceLanguage,  // e.g., "en"
                ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Translation failure must NOT fail the Create. Log and continue.
            logger.LogWarning(ex, "Translation orchestration failed for TourId={TourId}. Background service will retry.", evt.TourId);
        }

        // 2. Write integration event to outbox. UoW commits it with aggregate + translations.
        var integrationEvent = new TourCreatedIntegrationEvent(
            TourId: evt.TourId,
            Name: evt.Name,
            Slug: evt.Slug,
            CreatedByUserId: evt.CreatedByUserId,
            PlaceId: evt.PlaceId);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        // 3. NEVER call SaveChangesAsync here. UoW does it after all handlers complete.
        logger.LogInformation("Queued TourCreatedIntegrationEvent for TourId={TourId}", evt.TourId);
    }
}
```

#### `TourUpdatedDomainEventHandler`

```csharp
public async Task Handle(DomainEventNotification<TourUpdatedDomainEvent> note, CancellationToken ct)
{
    var evt = note.DomainEvent;

    // 1. Re-translate IF identity-bearing fields changed (diffed in the event itself)
    if (evt.NameChanged || evt.DescriptionChanged || evt.ShortDescriptionChanged)
    {
        try
        {
            await orchestrator.RefreshTranslationsAsync("Tour", evt.TourId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Translation refresh failed for TourId={TourId}", evt.TourId);
        }
    }

    // 2. Outbox
    var integrationEvent = new TourUpdatedIntegrationEvent(evt.TourId, evt.Name, evt.Description, evt.PlaceId);
    dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
}
```

**Convention**: update the domain event record to expose the "what changed" bool flags — handler checks them instead of re-loading the entity. The aggregate method populates them:

```csharp
// In Tour.Update(...)
var nameChanged = Name != newName;
// ... compute all flags ...
AddDomainEvent(new TourUpdatedDomainEvent(
    Id, newName, newDescription, PlaceId,
    NameChanged: nameChanged,
    DescriptionChanged: ... ,
    ShortDescriptionChanged: ...,
    PlaceIdChanged: ...));
```

#### `TourApprovedDomainEventHandler`

```csharp
public async Task Handle(DomainEventNotification<TourApprovedDomainEvent> note, CancellationToken ct)
{
    // Pure outbox-write handler. No side effects in this module.
    var evt = note.DomainEvent;
    var integrationEvent = new TourApprovedIntegrationEvent(
        TourId: evt.TourId,
        CreatedByUserId: evt.CreatedByUserId,  // Event carries this so Messaging can notify without cross-module DB query
        ApprovedAt: evt.ApprovedAt);
    dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
    logger.LogInformation("Tour approved: TourId={TourId} ApprovedBy={ReviewerId}", evt.TourId, evt.ReviewedByUserId);
}
```

Same shape for `Rejected`, `Suspended`, `Reinstated`, `FeaturedChanged` handlers. They differ only in the integration event record.

#### Integration events emitted by this module (for other modules to CONSUME — document in each event's XML-doc):

```csharp
// ContentTours.Contracts/IntegrationEvents/TourCreatedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentTours.Contracts.IntegrationEvents;

/// <summary>
/// Raised when a new Tour is created (status Draft).
/// Consumers: ContentSeo (create pending metadata stub), Analytics (popularity init), Messaging (none yet).
/// </summary>
public sealed record TourCreatedIntegrationEvent(
    Guid TourId,
    string Name,
    string Slug,
    Guid CreatedByUserId,
    Guid? PlaceId) : IntegrationEventBase;

/// <summary>
/// Raised when Tour identity-bearing fields change.
/// Consumers: ContentSeo (slug redirect if slug changed), Analytics (invalidate popularity cache).
/// </summary>
public sealed record TourUpdatedIntegrationEvent(
    Guid TourId,
    string Name,
    string? Description,
    Guid? PlaceId) : IntegrationEventBase;

/// <summary>
/// Raised on soft-delete. Handler WRITES DIRECTLY (no domain event — delete is a handler-level op on AuditableEntity).
/// Consumers: Booking (block new bookings), Social (cleanup favorites), ContentSeo (remove from sitemap).
/// </summary>
public sealed record TourDeletedIntegrationEvent(Guid TourId) : IntegrationEventBase;

/// <summary>
/// Raised when a provider submits a Draft tour for approval.
/// Consumers: Messaging (alert admin approval queue).
/// </summary>
public sealed record TourSubmittedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    DateTime SubmittedAt) : IntegrationEventBase;

/// <summary>
/// Raised when admin approves a Pending tour.
/// Consumers: Messaging (notify provider), ContentSeo (index), Analytics (enable tracking).
/// </summary>
public sealed record TourApprovedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    DateTime ApprovedAt) : IntegrationEventBase;

/// <summary>
/// Raised when admin rejects a Pending tour.
/// Consumers: Messaging (notify provider with reason).
/// </summary>
public sealed record TourRejectedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    string Reason,
    DateTime RejectedAt) : IntegrationEventBase;

/// <summary>
/// Raised when admin suspends an Approved tour.
/// Consumers: Messaging (notify provider), Booking (block new bookings), ContentSeo (de-index).
/// </summary>
public sealed record TourSuspendedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    string Reason,
    DateTime SuspendedAt) : IntegrationEventBase;

/// <summary>
/// Raised when admin reinstates a Suspended tour.
/// Consumers: Messaging, ContentSeo (re-index), Booking (unblock).
/// </summary>
public sealed record TourReinstatedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    DateTime ReinstatedAt) : IntegrationEventBase;
```

Register every integration event in `SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs` with a stable logical name (Outbox Hardening PR 2 requirement — `agent-context.md` §N):
```
content-tours.tour.created.v1
content-tours.tour.updated.v1
content-tours.tour.deleted.v1
content-tours.tour.submitted.v1
content-tours.tour.approved.v1
content-tours.tour.rejected.v1
content-tours.tour.suspended.v1
content-tours.tour.reinstated.v1
```

### WBS — Task 1 (44 hrs · M=Mon, T=Tue, W=Wed, R=Thu, F=Fri · Week 1)

| # | Sub-deliverable | Owner | Est. hrs | Finish by |
|---|---|---|---|---|
| 1.1 | Pre-work PW-1 + PW-2 (enum migration + UoW fix + sanity test) | Mahmoud | 4 | M 2026-05-04 17:00 |
| 1.2 | Domain: add `Create`, `Update`, `SoftDelete`, `Submit`, `Approve`, `Reject`, `Suspend`, `Reinstate`, `SetFeatured`, `UpdateRating`, `UpdateBookingCount` on `Tour.cs`. Add `TourCreatedDomainEvent`, `TourUpdatedDomainEvent`, `TourApprovedDomainEvent`, `TourRejectedDomainEvent`, `TourSuspendedDomainEvent`, `TourReinstatedDomainEvent`. Create `ITourRepository`. | Mahmoud | 6 | T 2026-05-05 13:00 |
| 1.3 | Contracts: `ContentToursFeatures.cs`, `ContentToursPermissionCatalog.cs`, integration events (`TourCreatedIntegrationEvent`, `TourUpdatedIntegrationEvent`, `TourDeletedIntegrationEvent`, `TourApprovedIntegrationEvent`, `TourRejectedIntegrationEvent`, `TourSuspendedIntegrationEvent`). Register catalog in DI. | Mahmoud | 3 | T 2026-05-05 17:00 |
| 1.4 | Application: `CreateTour` + `UpdateTour` (command + handler + validator + DTO) | Mahmoud | 5 | W 2026-05-06 13:00 |
| 1.5 | Application: `DeleteTour` + `SubmitTour` (gated) — compose cross-module `IPlaceExistsService` + `IAttachmentQueryService` checks | Mahmoud | 6 | W 2026-05-06 18:00 |
| 1.6 | Application: `ApproveTour`, `RejectTour`, `SuspendTour`, `ReinstateTour` (4 commands, admin-only) | Mahmoud | 5 | R 2026-05-07 13:00 |
| 1.7 | Application: `ListTours` (filters + sort + pagination), `GetTourById`, `GetTourBySlug`. Cache keys + `ICacheableQuery`. | Mahmoud | 5 | R 2026-05-07 18:00 |
| 1.8 | Infrastructure: `TourRepository`, DI registration (both UoW types, catalog, repo), outbox-write in 6 domain event handlers | Mahmoud | 5 | F 2026-05-08 13:00 |
| 1.9 | Presentation: 11 endpoints in `ContentToursEndpoints.cs` with correct `MustHavePermission`, `.Produces`, `.ProducesValidationProblem`, `.ProducesProblem(404/409/422)` | Mahmoud | 3 | F 2026-05-08 15:00 |
| 1.10 | Self-review + `dotnet build` + smoke test via Swagger + open PR | Mahmoud | 2 | F 2026-05-08 17:00 |

### Edge Cases to Handle

- Slug collision on create / update (check `ExistsBySlugAsync` / `ExistsBySlugExcludingAsync`)
- Submit when status is `Approved` — `Tour.InvalidTransition`
- Resubmit after Reject — allowed (Rejected → Draft auto-transition happens when owner calls `UpdateTour`; add guard in `UpdateTour` to reset status)
- `ApproveTour` on a `Draft`/`Rejected`/`Suspended` tour → `Tour.InvalidTransition`
- Delete when `BookingCount > 0` → `Tour.DeleteBlocked`
- `PlaceId` = deleted place on Create / Update → `Tour.PlaceDeleted`
- Concurrency: two admins approve simultaneously → one wins via RowVersion, other gets `Tour.ConcurrencyConflict` (409)

### Validator Rules (`CreateTourCommandValidator`)

```
Name:               .NotEmpty().MaximumLength(300)
Slug:               .MaximumLength(300).Matches(@"^[a-z0-9\-]+$").When(x => x.Slug != null)
Description:        .MaximumLength(4000).When(x => x.Description != null)
ShortDescription:   .MaximumLength(1000).When(x => x.ShortDescription != null)
Difficulty:         .IsInEnum()
DurationMinutes:    .GreaterThan(0).LessThanOrEqualTo(24 * 60 * 30)   // max 30 days
MaxGroupSize:       .GreaterThan(0).LessThanOrEqualTo(500)
BasePrice:          .GreaterThanOrEqualTo(0)
Currency:           .NotEmpty().Length(3)
Location.Latitude:  .InclusiveBetween(-90, 90)
Location.Longitude: .InclusiveBetween(-180, 180)
PlaceId:            .NotEqual(Guid.Empty).When(x => x.PlaceId.HasValue)
CancellationPolicyHours: .InclusiveBetween(0, 7 * 24)
MinAge / AgeRestriction: .InclusiveBetween(0, 120)
DiscountPercent:    .InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue)
```

---

## Task 2 — Mohammad · TourSchedule + TourPricingTier

**8 endpoints · 28 hours · Deadline: Thu 2026-05-14 · 17:00**
**Depends on: Task 1 (Tour must exist). Earliest start: W 2026-05-06 afternoon.**

### 🎯 Entities Touched

| Entity | Base Class | Role | Mohammad's Responsibility |
|---|---|---|---|
| **`TourSchedule`** | `BaseEntity` | **Non-aggregate child** of `Tour` | Own the full lifecycle. Add domain methods: `Create(tourId, dayOfWeek, startTime, endTime?, isActive)`, `Update(...)`, `Deactivate()`. Handler implements the **recurrence expansion engine** (Once / Daily / Weekly / Custom patterns → 1..N rows, 90-day cap, 120-row hard limit). Handler runs the **per-day overlap validation algorithm** before insert. **No domain events** (non-aggregate) — publish `TourScheduleChangedIntegrationEvent` optionally via direct outbox write. Hard delete (no `IsDeleted`), no `RowVersion`. Managed via `EfEntityRepository<TourSchedule, Guid>` or direct `DbContext`. |
| **`TourPricingTier`** | `BaseEntity` | **Non-aggregate child** of `Tour` | Own the full lifecycle. Add domain methods: `Create(tourId, name, description?, price, currency, minParticipants, maxParticipants?)`, `Update(...)`, `Deactivate()`. Handler enforces: (1) **currency match** with parent Tour's currency, (2) **"Adult" tier is magic** — cannot delete/deactivate the last active Adult tier on a Pending/Approved tour, (3) name uniqueness case-insensitive per tour. **No domain events** (non-aggregate) — publish `TourPricingTierChangedIntegrationEvent` directly via outbox. Hard delete only, no `RowVersion`. |
| `Tour` | `AuditableEntity, IAggregateRoot` (owned by Task 1) | Read-only from Mohammad's perspective | Loaded for ownership check (`tour.CreatedByUserId == currentUser.UserId`) and currency-coherence check. Mohammad does NOT mutate `Tour` fields; he only reads them. |

> **Key rule**: Neither `TourSchedule` nor `TourPricingTier` raises domain events. Use `IContentToursUnitOfWork` (plain SaveChanges, no event dispatch) — events are silently ignored anyway. Write integration events directly via `dbContext.OutboxMessages.Add(...)` BEFORE `SaveChangesAsync` so the outbox row commits atomically with the entity change.

### Task 2A — TourSchedule (4 endpoints)

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 12 | GET | `/api/v1/tours/{id:guid}/schedules` | Anonymous | ListTourSchedules |
| 13 | POST | `/api/v1/tours/{id:guid}/schedules` | `Tour.Update` | CreateTourSchedule (with recurrence expand) |
| 14 | PUT | `/api/v1/tours/{id:guid}/schedules/{scheduleId:guid}` | `Tour.Update` | UpdateTourSchedule |
| 15 | DELETE | `/api/v1/tours/{id:guid}/schedules/{scheduleId:guid}` | `Tour.Update` | DeleteTourSchedule |

### Task 2B — TourPricingTier (4 endpoints)

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 16 | GET | `/api/v1/tours/{id:guid}/pricing` | Anonymous | ListTourPricingTiers |
| 17 | POST | `/api/v1/tours/{id:guid}/pricing` | `Tour.Update` | CreateTourPricingTier |
| 18 | PUT | `/api/v1/tours/{id:guid}/pricing/{tierId:guid}` | `Tour.Update` | UpdateTourPricingTier |
| 19 | DELETE | `/api/v1/tours/{id:guid}/pricing/{tierId:guid}` | `Tour.Update` | DeleteTourPricingTier |

### Business Rules — TourSchedule

#### B1. Domain Invariants

1. **Schedules belong to a non-deleted Tour**: on `POST` the handler must load the parent tour and verify `!Tour.IsDeleted`. If tour is deleted → `Tour.NotFound`. If tour is `Suspended` or `Archived`, schedule operations are allowed (provider prepares for reinstatement) but logged at Warning level.
2. **`DayOfWeek` semantics**: byte 0..6 maps to `System.DayOfWeek` (0 = Sunday, 6 = Saturday). Always convert via `(byte)dayOfWeek` — never cast an int directly from the request.
3. **`TimeOnly` is UTC-naive**: `StartTime` and `EndTime` represent WALL-CLOCK time in the tour's local timezone (Asia/Amman for Jordan-based tours). Do NOT attempt to convert to UTC — the schedule says "every Monday at 09:00 local". The Booking module handles timezone-aware slot instantiation later.
4. **Open-ended schedule**: `EndTime == null` means "no fixed end" (guide decides). Allowed. Validator must NOT require EndTime.
5. **Active schedules only participate in submit-gate**: `Tour.Submit` gate from Task 1 checks `schedules.Any(s => s.IsActive)`. Deactivated schedules are ignored.
6. **Non-aggregate**: `TourSchedule : BaseEntity`. No domain events. No `IsDeleted` (hard delete). No RowVersion. Managed via `ContentToursDbContext` direct or a thin `EfEntityRepository<TourSchedule, Guid>`.

#### B2. Recurrence Pattern Specification (the HARD part)

The `POST /tours/{id}/schedules` endpoint accepts a **recurrence descriptor**, not a single schedule row. The handler EXPANDS it into 1..N `TourSchedule` rows in a single transaction.

**Request shape**:
```json
{
  "pattern": "Weekly",                      // Once | Daily | Weekly | Custom
  "daysOfWeek": [1, 3, 5],                  // required when pattern == Weekly; byte[0..6]
  "customDates": null,                      // required when pattern == Custom; DateOnly[]
  "startTime": "09:00:00",
  "endTime": "13:00:00",                    // nullable (open-ended)
  "validFrom": "2026-06-01",                // nullable; defaults to today
  "validTo": "2026-08-31",                  // nullable; defaults to validFrom + 90 days
  "isActive": true
}
```

**Expansion algorithm** (pseudocode):
```
validFrom  := request.ValidFrom  ?? DateOnly.FromDateTime(DateTime.UtcNow);
validTo    := request.ValidTo    ?? validFrom.AddDays(90);
cap        := DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90);
validTo    := MIN(validTo, cap);                          // 90-day hard cap

switch request.Pattern:
    case Once:
        requireSingleDate(request.CustomDates ?? [validFrom]);
        emit(day: that date's DayOfWeek, startTime, endTime);

    case Daily:
        for d in validFrom..validTo:
            emit(day: d.DayOfWeek, startTime, endTime);

    case Weekly:
        require request.DaysOfWeek.Any();
        for d in validFrom..validTo:
            if (byte)d.DayOfWeek in request.DaysOfWeek:
                emit(day: d.DayOfWeek, startTime, endTime);

    case Custom:
        require request.CustomDates.Any() && all >= today && all <= cap;
        for d in request.CustomDates:
            emit(day: d.DayOfWeek, startTime, endTime);
```

**Idempotency rule**: on emit, skip if `dbContext.TourSchedules.AnyAsync(s => s.TourId == tourId && s.DayOfWeek == day && s.StartTime == startTime, ct)`. Returns a result payload `{ created: int, skipped: int }` so the caller sees what happened. This makes the endpoint safe to retry.

**Expansion output limit**: if the pattern would generate more than **120 rows** in one call → return `TourSchedule.ExpansionTooLarge` 422 with hint to narrow `validTo`. Protects against `pattern=Daily, validFrom=today, validTo=today+10000`.

#### B3. Overlap Validation Algorithm

Per `DayOfWeek`, no two ACTIVE rows may have overlapping `[StartTime, EndTime]` intervals. Open-ended (`EndTime == null`) blocks the entire rest of the day.

Algorithm (runs in the handler BEFORE emit, on the combined set of existing + proposed):
```
for each dayOfWeek with candidate rows:
    sorted := rows.OrderBy(StartTime);
    for i in 0..sorted.Count - 2:
        a := sorted[i];
        b := sorted[i+1];
        endA := a.EndTime ?? TimeOnly.MaxValue;
        if b.StartTime < endA:
            fail TourSchedule.OverlapDetected with payload { dayOfWeek, a, b };
```

Error payload carries both conflicting rows so the UI can highlight them. Return 422 not 400 (semantic validation, not shape validation).

#### B4. Authorization Matrix

| Endpoint | Who | Permission |
|---|---|---|
| `GET /tours/{id}/schedules` | Anonymous — public. Returns ACTIVE rows only. | none |
| `POST /tours/{id}/schedules` | Owner of parent tour OR admin | `ContentTours.Tour.Update` |
| `PUT /tours/{id}/schedules/{sid}` | Owner of parent tour OR admin | `ContentTours.Tour.Update` |
| `DELETE /tours/{id}/schedules/{sid}` | Owner of parent tour OR admin (with booking-count check) | `ContentTours.Tour.Update` |

Owner check: handler loads `tour`, compares `tour.CreatedByUserId == currentUser.UserId.Value`. Non-owner non-admin → `Tour.NotOwner` 403. Tour soft-deleted → `Tour.NotFound` 404 (do NOT leak its existence to non-admin).

#### B5. Deletion Guard (Cross-Module Hook)

- Before hard-deleting a schedule, handler calls `await bookingCountService.GetFutureBookingCountForScheduleAsync(scheduleId, ct)`.
- The interface lives in `ContentTours.Application/Interfaces/IScheduleBookingCountService.cs`. Ship a stub implementation in `ContentTours.Infrastructure/Services/NoOpScheduleBookingCountService.cs` that returns `0` and log `Debug("No booking module; returning 0 for scheduleId={id}")`.
- Future Booking module replaces the stub registration with a real implementation that queries `bookings WHERE ScheduleId = X AND StartsAt > UtcNow AND Status IN (Confirmed, PendingConfirmation, AwaitingPayment)`.
- If count > 0 → `TourSchedule.DeleteBlocked` 409 with `{ futureBookings: N }` in payload.

#### B6. Error Code Catalog (Task 2A — Schedules)

| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | Parent tour missing / soft-deleted |
| `Tour.NotOwner` | 403 | IDOR — not owner and not admin |
| `TourSchedule.NotFound` | 404 | Schedule row missing under this tour |
| `TourSchedule.InvalidDayOfWeek` | 400 | Byte outside 0..6 |
| `TourSchedule.InvalidTimeRange` | 400 | `EndTime` ≤ `StartTime` when both set |
| `TourSchedule.OverlapDetected` | 422 | B3 algorithm failed; payload carries the conflicting rows |
| `TourSchedule.PatternParamsInvalid` | 400 | e.g., `Pattern=Weekly` but `DaysOfWeek` empty, or `Pattern=Custom` with no dates |
| `TourSchedule.ExpansionTooLarge` | 422 | Would produce > 120 rows in one call |
| `TourSchedule.CustomDateOutOfRange` | 422 | Custom date is in past or beyond 90 days |
| `TourSchedule.DeleteBlocked` | 409 | Future bookings reference this schedule |
| `TourSchedule.ConcurrencyConflict` | 409 | Same user modified the schedule in parallel (rare — no RowVersion) |

#### B7. Cache Policy (Task 2A)

| Query | Key | TTL | Tags |
|---|---|---|---|
| `ListTourSchedules` | `ct:tour-schedules:{tourId}:active={activeOnly}` | 5 min | `tour-schedules:{tourId}`, `tour:{tourId}` |

**Invalidation**:
- POST/PUT/DELETE schedule → `RemoveByTagAsync("tour-schedules:{tourId}")` AND `RemoveByTagAsync("tour:{tourId}")` (detail DTO embeds schedule summary).
- Never evict the coarse `tour-schedules` tag in a single-tour mutation.

### Business Rules — TourPricingTier

#### B1. Domain Invariants

1. **Tier Name is free-text business vocabulary** — not an enum. Examples: `"Adult"`, `"Child 3-12"`, `"Infant 0-2"`, `"Senior 65+"`, `"Student"`, `"Family Pack"`, `"Group 10+"`. Case-insensitive uniqueness per tour.
2. **Currency must match parent Tour's currency**: on create/update, load the tour and check `tier.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase)`. Mismatch → `TourPricingTier.CurrencyMismatch` 400. ALL tiers on a tour share one currency.
3. **"Adult" tier is magic**: Task 1's pre-submit gate checks `tiers.Any(t => t.IsActive && t.Name.Equals("Adult", StringComparison.OrdinalIgnoreCase))`. A tour CANNOT be submitted without at least one active Adult tier. Deletion or deactivation of the last Adult tier is blocked with `TourPricingTier.AdultTierRequired` 409 if tour is currently `Approved` or `Pending`. Allowed if tour is `Draft` (provider still editing).
4. **Free tiers allowed**: `Price.Amount >= 0`. A tier of `Price = 0` represents "free for this demographic" (e.g., infants). Not the same as deactivation.
5. **Participant bounds coherence**: `MinParticipants >= 1`. `MaxParticipants == null` means "unbounded". `MaxParticipants > MinParticipants` if both set. `MinParticipants > 1` only makes sense for group tiers (naming convention like `"Group 10+"`).
6. **Tier deactivation (PUT with IsActive=false)** is reversible — sets `IsActive = false`, keeps the row. Deactivating the last active Adult tier on a Pending/Approved tour → same block as deletion.
7. **Non-aggregate**: `TourPricingTier : BaseEntity`. No domain events. No soft-delete — hard delete only. No RowVersion.

#### B2. Authorization Matrix

| Endpoint | Who | Permission |
|---|---|---|
| `GET /tours/{id}/pricing` | Anonymous. Returns ACTIVE only for public; all-tiers for owner/admin (detect via `HttpContext.User.Identity.IsAuthenticated && ownerOrAdmin`) | none |
| `POST /tours/{id}/pricing` | Owner or admin | `ContentTours.Tour.Update` |
| `PUT /tours/{id}/pricing/{tid}` | Owner or admin | `ContentTours.Tour.Update` |
| `DELETE /tours/{id}/pricing/{tid}` | Owner or admin (with Adult-tier guard) | `ContentTours.Tour.Update` |

#### B3. Error Code Catalog (Task 2B)

| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | Parent missing |
| `Tour.NotOwner` | 403 | IDOR |
| `TourPricingTier.NotFound` | 404 | Tier id not under this tour |
| `TourPricingTier.NameConflict` | 409 | Case-insensitive duplicate name on same tour |
| `TourPricingTier.CurrencyMismatch` | 400 | Currency ≠ parent tour's currency |
| `TourPricingTier.InvalidPrice` | 400 | Price < 0 |
| `TourPricingTier.InvalidParticipantRange` | 400 | Bounds violate B1.5 |
| `TourPricingTier.AdultTierRequired` | 409 | Last Adult tier delete/deactivate on non-Draft tour |
| `TourPricingTier.ConcurrencyConflict` | 409 | Rare — parallel mutation |

#### B4. Cache Policy

| Query | Key | TTL | Tags |
|---|---|---|---|
| `ListTourPricingTiers` | `ct:tour-pricing:{tourId}:active={activeOnly}:lang:{Accept-Language}` | 10 min | `tour-pricing:{tourId}`, `tour:{tourId}` |

Invalidate `tour-pricing:{tourId}` + `tour:{tourId}` on any mutation. Finance module later consumes `TourPricingTierChangedIntegrationEvent` (see handler logic below) to recompute `Tour.SalePrice`.

#### B5. Acceptance Test Scenarios

1. Owner creates Adult tier, Child tier, submits tour → submit passes (pricing gate green).
2. Owner creates Child tier only, submits → submit fails with `Tour.NoAdultPricingTier` 422.
3. Owner deletes last Adult tier on Approved tour → 409 `TourPricingTier.AdultTierRequired`.
4. Owner deletes last Adult tier on Draft tour → 204 No Content (allowed).
5. Owner creates two tiers both named `"Adult"` → second fails with `TourPricingTier.NameConflict` 409.
6. Owner creates tier with currency `"USD"` on a JOD tour → 400 `TourPricingTier.CurrencyMismatch`.
7. Admin PATCHes tier `IsActive=false` on last Adult of Pending tour → 409.

### Domain & Integration Event Handler Logic (Task 2)

**Neither `TourSchedule` nor `TourPricingTier` raises domain events** (non-aggregates). To notify other modules, the handler writes the integration event DIRECTLY to `dbContext.OutboxMessages` BEFORE calling `SaveChangesAsync`.

#### Pattern (apply in every Task 2 command handler that needs to notify)

```csharp
public async Task<Result<CreateTourPricingTierResult>> Handle(
    CreateTourPricingTierCommand cmd, CancellationToken ct)
{
    // 1. Load parent, ownership + currency checks
    var tour = await tourRepository.GetByIdAsync(cmd.TourId, ct);
    if (tour is null || tour.IsDeleted) return NotFound(...);
    if (tour.CreatedByUserId != currentUser.UserId && !currentUser.IsInRole("Admin"))
        return Forbidden(...);
    if (!tier.Currency.Equals(tour.Currency, StringComparison.OrdinalIgnoreCase))
        return BadRequest("TourPricingTier.CurrencyMismatch");

    // 2. Create the entity
    var tier = TourPricingTier.Create(tour.Id, cmd.Name, cmd.Description,
        new Money(cmd.Price, cmd.Currency), cmd.Currency,
        cmd.MinParticipants, cmd.MaxParticipants);
    await tierRepository.AddAsync(tier, ct);

    // 3. Write outbox row — UoW commits atomically with the tier insert
    var integrationEvent = new TourPricingTierChangedIntegrationEvent(
        TierId: tier.Id, TourId: tour.Id, NewPrice: tier.Price.Amount, Currency: tier.Currency,
        ChangeType: "Created");
    dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

    // 4. Save (IContentToursUnitOfWork is fine here — no domain events)
    await unitOfWork.SaveChangesAsync(ct);

    // 5. Cache invalidation AFTER save succeeds
    await cache.RemoveByTagAsync($"tour-pricing:{tour.Id}", ct);
    await cache.RemoveByTagAsync($"tour:{tour.Id}", ct);

    logger.LogInformation("Created TourPricingTier {TierId} on TourId={TourId} at {Price} {Currency}",
        tier.Id, tour.Id, tier.Price.Amount, tier.Currency);

    return Result<CreateTourPricingTierResult>.Created(new(tier.Id, tier.Name));
}
```

#### Integration events emitted by Task 2

```csharp
// ContentTours.Contracts/IntegrationEvents/TourPricingTierChangedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentTours.Contracts.IntegrationEvents;

/// <summary>
/// Raised on Create/Update/Delete of a TourPricingTier.
/// Consumers: Finance (recompute Tour.SalePrice if discount active), Analytics (price-change tracking).
/// </summary>
public sealed record TourPricingTierChangedIntegrationEvent(
    Guid TierId,
    Guid TourId,
    decimal NewPrice,
    string Currency,
    string ChangeType        // "Created" | "Updated" | "Deleted" | "Deactivated"
) : IntegrationEventBase;

/// <summary>
/// Optional — raised when a schedule is created/modified.
/// Consumers: Booking (invalidate availability cache), Analytics (tour-activity tracking).
/// Ship only if Booking team requests it; otherwise skip.
/// </summary>
public sealed record TourScheduleChangedIntegrationEvent(
    Guid ScheduleId,
    Guid TourId,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    string ChangeType        // "Created" | "Updated" | "Deleted"
) : IntegrationEventBase;
```

Register in `IntegrationEventTypeRegistry`:
```
content-tours.pricing-tier.changed.v1
content-tours.schedule.changed.v1    (only if shipped)
```

### WBS — Task 2 (28 hrs · spans W2 W/R)

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 2.1 | Domain: `TourSchedule.Create/Update/Deactivate`, `TourPricingTier.Create/Update/Deactivate`, repo interfaces (or skip — use `EfEntityRepository<T,Guid>`) | 3 | W 2026-05-06 18:00 |
| 2.2 | Application: `ListTourSchedules` + `CreateTourSchedule` with recurrence expansion (loops + overlap check) | 6 | R 2026-05-07 17:00 |
| 2.3 | Application: `UpdateTourSchedule` + `DeleteTourSchedule` (with booking-count hook) | 4 | S 2026-05-10 13:00 |
| 2.4 | Application: `ListTourPricingTiers`, `CreateTourPricingTier` (Adult-tier-aware), `UpdateTourPricingTier`, `DeleteTourPricingTier` (last-Adult guard) | 6 | S 2026-05-10 18:00 |
| 2.5 | Infrastructure: DI wiring + cache tag design (`tour-schedules:{tourId}`, `tour-pricing:{tourId}`) | 2 | M 2026-05-11 11:00 |
| 2.6 | Presentation: 8 endpoints | 3 | M 2026-05-11 15:00 |
| 2.7 | Validator dossier for overlap & Adult-tier rules | 2 | M 2026-05-11 17:00 |
| 2.8 | Self-review + `dotnet build` + smoke test + PR | 2 | T 2026-05-12 12:00 |

### Validator Rules

```
-- TourSchedule --
DayOfWeek:     .InclusiveBetween(0, 6)
StartTime:     .NotEmpty()
EndTime:       .GreaterThan(x => x.StartTime).When(x => x.EndTime.HasValue)
IsActive:      (no validation — boolean)
-- Recurrence pattern --
Pattern:       .IsInEnum()  // Once / Daily / Weekly / Custom
DaysOfWeek:    .NotEmpty().When(Pattern == Weekly)  // List<byte> 0..6
CustomDates:   .NotEmpty().When(Pattern == Custom) — each date >= Today, <= Today + 90
ValidTo:       .GreaterThan(x => DateTime.UtcNow).When(x => x.ValidTo.HasValue)

-- TourPricingTier --
Name:              .NotEmpty().MaximumLength(200)
Description:       .MaximumLength(500).When(x => x.Description != null)
Price:             .GreaterThanOrEqualTo(0)
Currency:          .NotEmpty().Length(3)
MinParticipants:   .GreaterThanOrEqualTo(1)
MaxParticipants:   .GreaterThan(x => x.MinParticipants).When(x => x.MaxParticipants.HasValue)
```

---

## Task 3 — Mohammad · Search + Featured + MyTours

**5 endpoints · 24 hours · Deadline: Mon 2026-05-18 · 17:00**
**Depends on: Task 1. Can start in parallel with Task 2B once Task 1 lists are done.**

### 🎯 Entities Touched

| Entity | Base Class | Role | Mohammad's Responsibility |
|---|---|---|---|
| `Tour` | `AuditableEntity, IAggregateRoot` (owned by Task 1) | **Read-heavy** for search/featured/my-tours; **single mutation** via `SetFeatured` | **4 of 5 endpoints are read-only** (Search, Suggest, Featured, MyTours) — no domain mutations, just complex LINQ projections. The 5th endpoint (`ToggleTourFeatured`) calls `Tour.SetFeatured(isFeatured, changedByUserId)` which must be added to the aggregate (owned by Task 1 but added here if Mahmoud didn't). Raises `TourFeaturedChangedDomainEvent` ONLY when value actually changes (idempotency per ERR-009). |
| `TourTranslation` | `BaseEntity` (owned by Task 1) | Read-only | Search tokenizer matches `Tour.Name` OR `TourTranslation.Name` for the requested `Accept-Language`. Autocomplete (`SuggestTours`) also joins this. |

> **Key rule**: Task 3 is **95% reads**. The only write is `ToggleTourFeatured` — use `IUnitOfWork<ContentToursDbContext>` there because it raises a domain event. All 4 read queries implement `ICacheableQuery` with fine-grained tags per `B10. Cache Policy`.
>
> **No new entities created.** Task 3 writes no new schema — it's search logic, facet computation, and a curation flag toggle.

### Endpoints

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 20 | GET | `/api/v1/tours/search` | Anonymous | SearchTours (full-text + faceted) |
| 21 | GET | `/api/v1/tours/search/suggest` | Anonymous | SuggestTours (autocomplete) |
| 22 | GET | `/api/v1/tours/featured` | Anonymous | ListFeaturedTours |
| 23 | GET | `/api/v1/tours/provider/my-tours` | `Tour.ReadOwn` | ListMyTours (owner sees all statuses) |
| 24 | PATCH | `/api/v1/tours/admin/{id:guid}/feature` | `Tour.Feature` | ToggleTourFeatured |

### Business Rules

#### B1. SearchTours — Tokenization Spec

1. Read `q` from query string. If null/whitespace AND zero filters specified → reject `Tour.SearchQueryRequired` 400 (prevents unindexed table scans).
2. Trim + `ToLowerInvariant()`.
3. Split on whitespace (`\s+` regex).
4. Drop tokens shorter than 2 chars (`"a"`, `"to"` are noise).
5. Drop duplicate tokens (HashSet).
6. Hard cap at 10 tokens per query — `q=` with more is truncated (log at Debug, don't fail).
7. For Arabic queries, do NOT normalize diacritics (`ḍ/ض` differences matter). SQL Server `LIKE` with default `SQL_Latin1_General_CP1_CI_AS` is case-insensitive AND accent-insensitive — acceptable for v1.
8. If the full `q` (after trim) has zero surviving tokens → treat as "no search query" (paginate all filtered matches).

#### B2. SearchTours — Ranking Formula (`sort=relevance`)

Compound score computed in a `.Select(...)` projection so it runs entirely in SQL (EF-translatable). Pseudo-SQL:

```
SCORE =
     3 * (tokens matched in Name or Translation.Name — count)
   + 1 * (tokens matched in Description or ShortDescription or Translation.Description — count)
   + 0.3 * COALESCE(AverageRating, 0)           -- 0..5 range → 0..1.5 boost
   + LOG10(BookingCount + 1)                    -- 0..4 for up to 10k bookings
   + 1.0 / (1 + DaysSinceCreated / 30.0)        -- recency boost, decays monthly
```

Order by `SCORE DESC, CreatedAt DESC` (stable tiebreak). Cap score computation at `SearchDocumentMaxTokens = 10` to bound SQL complexity.

**Implementation note**: express token-matched counts as `((name LIKE '%token1%' ? 1 : 0) + (name LIKE '%token2%' ? 1 : 0) + ...)`. If any token is NOT matched in any field → exclude row from results (AND semantic across all tokens).

**Other sort modes** bypass the score computation and use pure SQL ORDER BY:
- `price_asc` → `BasePrice ASC` (or `SalePrice ASC` if discount active)
- `price_desc` → symmetric
- `rating_desc` → `AverageRating DESC, ReviewCount DESC`
- `popularity_desc` → `BookingCount DESC, CreatedAt DESC`
- `newest` → `CreatedAt DESC`

Default: `relevance` when `q` is set, `popularity_desc` when only filters are set.

#### B3. SearchTours — Facet Computation

Response shape:
```json
{
  "items": [ /* TourSummaryDto[] */ ],
  "total": 237,
  "page": 1,
  "pageSize": 20,
  "totalPages": 12,
  "facets": {
    "priceBuckets": [
      { "min": 0,   "max": 50,  "count": 42 },
      { "min": 50,  "max": 100, "count": 88 },
      { "min": 100, "max": 200, "count": 71 },
      { "min": 200, "max": 500, "count": 26 },
      { "min": 500, "max": null,"count": 10 }
    ],
    "difficultyCounts":  { "Easy": 89, "Moderate": 102, "Hard": 36, "Expert": 10 },
    "ratingBuckets": [
      { "min": 4.5, "max": 5.0, "count": 66 },
      { "min": 4.0, "max": 4.5, "count": 91 },
      { "min": 3.0, "max": 4.0, "count": 54 },
      { "min": 0.0, "max": 3.0, "count": 26 }
    ],
    "isChildFriendlyCount":  58,
    "isAccessibleCount":     34,
    "isInstantBookingCount": 112
  },
  "appliedFilters": { /* echo back */ }
}
```

**Facet computation method**: after the main filter query but BEFORE pagination, execute the facet query on the SAME filtered set (not on all tours). Pattern:

```csharp
// 1. Build the filtered IQueryable<Tour> (with all filters applied, no paging, no sort)
var filtered = BuildFilteredQuery(request, dbContext.Tours, ct);

// 2. Snapshot rows for facet computation — cap at 5000 to bound memory
var facetRows = await filtered
    .Select(t => new FacetRow(t.BasePrice.Amount, t.Difficulty, t.AverageRating,
                              t.IsChildFriendly, t.IsAccessible, t.IsInstantBooking))
    .Take(5000)
    .ToListAsync(ct);

// 3. Compute facets in-memory (small, bounded)
var facets = ComputeFacets(facetRows);

// 4. Re-apply sort + paging for the items
var page = await filtered.OrderByDescending(...).Skip(skip).Take(take).Select(SummaryProjection).ToListAsync(ct);
```

Bucket boundaries for price are DYNAMIC: compute min/max of `facetRows`, then split into 5 equal-width buckets. If min == max (all same price), return a single bucket.

#### B4. SearchTours — Filter Composition

Every filter from Task 1's `ListTours` filter set applies to search, plus:

- `categoryId` (reserved, ContentCore Category — join TBD)
- `placeId` (exact match on `Tour.PlaceId`)
- `priceMin`, `priceMax` (inclusive, in tour's currency — no conversion)
- `difficulty` (single enum value, case-insensitive match on name)
- `durationMinutesMin`, `durationMinutesMax`
- `isChildFriendly` (nullable bool, null = any)
- `isAccessible` (nullable bool)
- `isInstantBooking` (nullable bool)
- `hasDiscount` (derived: `DiscountValidFrom <= UtcNow AND DiscountValidTo > UtcNow`)
- `minRating` (float, inclusive)
- `languageCode` (matches ANY `TourTranslation.LanguageCode` for this tour)

**Public-vs-admin visibility**: Search always enforces `Status == Approved AND !IsDeleted`. Admins see the same — for multi-status search use `/provider/my-tours` or a future `/admin/all`.

#### B5. SuggestTours — Autocomplete Spec

- Query: `q` (required, 2..100 chars, no filters).
- Match: `Tour.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase)` OR `TourTranslation.Name.StartsWith(q, ...)` for current `Accept-Language`.
- Scope: `Status == Approved AND !IsDeleted`.
- Limit: `.Take(10)`.
- Order: `BookingCount DESC, Name ASC`.
- Response: array of `{ id: Guid, name: string, slug: string, thumbnailUrl: string? }`.
- Latency target: p95 < 100ms. Add covering index suggestion (`IX_Tours_Name_Prefix`) in migration if needed — coordinate with Tech Lead.
- Cache: 30s TTL, keyed on `(q, Accept-Language)`. Tag `tours:suggest`.

#### B6. ListFeaturedTours — Editorial Logic

- Filter: `IsFeatured == true AND Status == Approved AND !IsDeleted`.
- Sort: `BookingCount DESC, AverageRating DESC` (popularity first, quality as tiebreak).
- Limit: 20 (hard-coded, no pagination).
- Cache: 10 min TTL. Tag `tours:featured`.
- No filters accepted (it's a curated list). If you need filtering, use Search.

#### B7. ListMyTours — Provider Dashboard

- **Endpoint is authenticated** (`Tour.ReadOwn` permission ⇒ any logged-in provider; admins with `Tour.ReadAny` see a global view).
- Handler behaviour depends on caller's role:
  - Admin with `Tour.ReadAny` permission → returns any tours matching filters (optional `providerUserId` query param).
  - Provider (any authenticated non-admin) → always filters `CreatedByUserId == currentUser.UserId.Value`. Ignores any `providerUserId` query param from body (IDOR prevention).
- Returns ALL statuses (Draft, Pending, Approved, Rejected, Suspended). Soft-deleted are excluded by default; `includeDeleted=true` requires admin.
- Pagination: default 20, max 100 (higher than public list — providers iterate their catalog).
- Sort: `sort` query param same as `ListTours`, plus `status_priority` (Draft→Pending→Approved→Rejected→Suspended, then `UpdatedAt DESC`).
- Cache: per-user cached is dangerous (any mutation must bust per-user key). TTL 2 min, tag `my-tours:{userId}`. Invalidate on any Tour create/update/delete by that user.

#### B8. ToggleTourFeatured — Admin Curation

- Endpoint: `PATCH /api/v1/tours/admin/{id}/feature` with body `{ "isFeatured": true | false }`.
- Admin-only (`ContentTours.Tour.Feature`).
- Tour must exist AND be `Status == Approved` AND `!IsDeleted`. Otherwise `Tour.CannotFeatureNonApproved` 409.
- Idempotent: flipping to the same value is a no-op — return `204 No Content` without raising the domain event (ERR-009 prevention).
- Raises `TourFeaturedChangedDomainEvent(TourId, IsFeatured, ChangedByUserId)` only when value actually changed.
- Cache invalidation: `tours:featured`, `tour:{id}`, `tours:list`, `tours:search`.

#### B9. Error Code Catalog (Task 3)

| Code | HTTP | When |
|---|---|---|
| `Tour.SearchQueryRequired` | 400 | `q` null AND no filters |
| `Tour.InvalidSort` | 400 | Sort value outside allowed enum |
| `Tour.SearchTokenTooShort` | — | Not returned — tokens <2 chars silently dropped |
| `Tour.NotFound` | 404 | Feature toggle target missing |
| `Tour.CannotFeatureNonApproved` | 409 | Feature toggle on non-Approved tour |
| `Tour.ReadOwnRequiresAuth` | 401 | MyTours called without auth (filter by permission — `MustHavePermission` already returns 403, this is defensive) |

#### B10. Cache Policy (Task 3)

| Query | Key | TTL | Tags |
|---|---|---|---|
| `SearchTours` | `ct:tours:search:{hash(q + filters + sort + page + pageSize + Accept-Language)}` | 2 min | `tours:search`, `tours:list` |
| `SuggestTours` | `ct:tours:suggest:{q}:lang:{Accept-Language}` | 30 sec | `tours:suggest`, `tours:list` |
| `ListFeaturedTours` | `ct:tours:featured:lang:{Accept-Language}` | 10 min | `tours:featured`, `tours:list` |
| `ListMyTours` | `ct:my-tours:{userId}:p{page}:s{pageSize}:status:{status}:sort:{sort}` | 2 min | `my-tours:{userId}` |

**Invalidation rules** (applied by Task 1 handlers too, noted here for cross-reference):
- Tour create/update/delete by user X → bust `my-tours:{X}` + `tours:search` + `tours:list` (+ `tours:featured` if `IsFeatured`).
- Tour feature toggle → `tours:featured` + `tours:search`.
- Tour status transition → all 4 tag families above.

#### B11. Acceptance Test Scenarios

1. Search with 2-word query "petra tour" → both tokens must match, results ordered by score.
2. Search with `sort=price_asc`, `priceMin=50` → returns cheapest first, all ≥ 50.
3. Search with no `q` and no filters → 400 `Tour.SearchQueryRequired`.
4. Suggest `"pet"` → returns up to 10 tours starting with "pet".
5. Featured list never returns a tour with `IsFeatured=false`.
6. Provider calls `/provider/my-tours` → sees only their tours, all statuses.
7. Admin calls `/provider/my-tours?providerUserId=X` → sees user X's tours.
8. Non-admin calls `/provider/my-tours?providerUserId=X` → sees their OWN tours (query param ignored, no 403 — silently scoped).
9. Toggle feature on a Draft tour → 409.
10. Toggle feature twice in a row → second call is idempotent no-op, no duplicate event.

### Domain & Integration Event Handler Logic (Task 3)

Only the feature-toggle endpoint raises a domain event.

#### `TourFeaturedChangedDomainEventHandler`

Logic:
- Cache bust at `tours:featured` (not done in the command handler because it's cleaner here — after UoW commit).
- Write `TourFeaturedChangedIntegrationEvent` to outbox.

```csharp
public sealed class TourFeaturedChangedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourFeaturedChangedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourFeaturedChangedDomainEvent>>
{
    public Task Handle(DomainEventNotification<TourFeaturedChangedDomainEvent> note, CancellationToken ct)
    {
        var evt = note.DomainEvent;
        var integrationEvent = new TourFeaturedChangedIntegrationEvent(
            TourId: evt.TourId,
            IsFeatured: evt.IsFeatured,
            ChangedByUserId: evt.ChangedByUserId,
            ChangedAt: evt.ChangedAt);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        logger.LogInformation("Tour {TourId} featured flag set to {IsFeatured} by {UserId}",
            evt.TourId, evt.IsFeatured, evt.ChangedByUserId);
        return Task.CompletedTask;
    }
}
```

#### Integration event

```csharp
/// <summary>
/// Raised when admin toggles IsFeatured on a Tour.
/// Consumers: Analytics (log editorial curation), ContentSeo (boost in sitemap priority).
/// </summary>
public sealed record TourFeaturedChangedIntegrationEvent(
    Guid TourId,
    bool IsFeatured,
    Guid ChangedByUserId,
    DateTime ChangedAt) : IntegrationEventBase;
```

Registry key: `content-tours.tour.featured-changed.v1`.

Search / Suggest / Featured / MyTours queries raise **no events** — they're reads.

### WBS — Task 3 (24 hrs)

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 3.1 | `ListMyTours` + `ListFeaturedTours` + `ToggleTourFeatured` (add `Tour.SetFeatured` domain method if not done in Task 1) | 6 | T 2026-05-12 18:00 |
| 3.2 | `SuggestTours` with prefix index strategy | 3 | W 2026-05-13 12:00 |
| 3.3 | `SearchTours` — tokenizer + ranking `.Select(...)` projection + filters + sort composition | 10 | R 2026-05-14 18:00 |
| 3.4 | `SearchTours` — facet computation in same request (second query on un-paginated set, capped at 5000 rows) | 3 | S 2026-05-17 13:00 |
| 3.5 | Presentation: 5 endpoints + test facets via Swagger + self-review + PR | 2 | M 2026-05-18 17:00 |

### Validator Rules

```
-- SearchTours --
Q:         .MaximumLength(200)  // nullable, but reject if null AND no filters (handled in handler)
Page:      .GreaterThanOrEqualTo(1)
PageSize:  .InclusiveBetween(1, 50)
Sort:      .IsInEnum()  // SearchSort enum

-- SuggestTours --
Q:         .NotEmpty().MinimumLength(2).MaximumLength(100)
```

---

## Task 4 — Ezz · TourWaypoint + TourTourGuide + ChildrenInfo

**9 endpoints · 36 hours · Deadline: Mon 2026-05-18 · 17:00**
**Depends on: Task 1. Earliest start: W 2026-05-06 afternoon.**

### 🎯 Entities Touched

| Entity | Base Class | Role | Ezz's Responsibility |
|---|---|---|---|
| **`TourWaypoint`** | `BaseEntity` | **Non-aggregate child** of `Tour` | Own the full lifecycle. Add domain methods: `Create(tourId, name, description?, location, waypointType, sortOrder, durationMinutes?)`, `Update(...)`, `SetSortOrder(int)` (package-private). Change `WaypointType` field from `byte` to a new `WaypointType` enum (`Start`, `Stop`, `Meal`, `Photo`, `Landmark`, `RestStop`, `End`). Implement the **dense SortOrder invariant** (0..N-1, no gaps) maintained on every add/remove/reorder inside a serializable transaction. No domain events, hard delete only, no `RowVersion` (accept concurrency trade-off per R-4). |
| **`TourTourGuide`** | **Pure junction** (no base class, composite PK `(TourId, TourGuideId)`, only `IsPrimary` bool) | **Non-aggregate junction** between `Tour` and Security's User | Own the full lifecycle. Add `Create(tourId, tourGuideUserId, isPrimary)`. Handler enforces: (1) **exactly-one-primary invariant** per tour (demote old primary on new claim, promote oldest on primary removal — deterministic by smallest `TourGuideId` since junction has no `CreatedAt`), (2) **role validation** via stub `IUserRoleChecker.HasRoleAsync(userId, "TourGuide", ct)`, (3) composite PK enforces no-duplicate-assignment. List endpoint is **public** (unlike ContentPlaces BusinessStaff). Write `TourGuideAssigned/UnassignedIntegrationEvent` directly to outbox. |
| `Tour` | `AuditableEntity, IAggregateRoot` (owned by Task 1) | **Task 4C (ChildrenInfo) mutates fields on Tour directly** — NO separate entity | Add `Tour.UpdateChildrenInfo(isChildFriendly, ageRestriction, minChildAge, maxChildAge, childFacilities)` domain method. Piggybacks on existing `TourUpdatedDomainEvent` with a new `ChildrenInfoChanged: bool` flag (add to Mahmoud's event record). Migration `AddTourChildrenInfoFields` adds 3 new columns to `Tour` table: `MinChildAge int?`, `MaxChildAge int?`, `ChildFacilities nvarchar(500)?`. Existing fields `IsChildFriendly` and `AgeRestriction` are reused. |
| `ChildFacility` (new enum) | `byte` enum in `ContentTours.Domain.Enums` | Controlled vocabulary for `Tour.ChildFacilities` CSV column | Ezz ships a small CSV parser that validates each token against this enum: `Stroller`, `HighChair`, `ChangingStation`, `ChildMenu`, `NursingRoom`, `ChildToilet`, `PlayArea`, `ChildSeat`, `BabyCarrier`, `AirConditioning`. Unknown token → `Tour.UnknownChildFacility` 400. |

> **Key rule**: Task 4C is **NOT a new entity** — it's fields on the `Tour` aggregate. Update Task 1's `TourUpdatedDomainEvent` record to add `ChildrenInfoChanged: bool = false` (backward-compatible default). `Tour.UpdateChildrenInfo()` raises this event with the flag = true, and Mahmoud's `TourUpdatedDomainEventHandler` writes `TourUpdatedIntegrationEvent` with the mirror flag so ContentSeo can re-index child-friendly facets.
>
> **Migration on Ezz's critical path**: `AddTourChildrenInfoFields` must land before endpoint work starts (see WBS 4.2). Also adds/updates the `WaypointType` enum conversion.

### Task 4A — TourWaypoint (4 endpoints)

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 25 | GET | `/api/v1/tours/{id:guid}/waypoints` | Anonymous | ListTourWaypoints |
| 26 | POST | `/api/v1/tours/{id:guid}/waypoints` | `Tour.Update` | AddTourWaypoint |
| 27 | PUT | `/api/v1/tours/{id:guid}/waypoints/reorder` | `Tour.Update` | ReorderTourWaypoints (batch) |
| 28 | DELETE | `/api/v1/tours/{id:guid}/waypoints/{waypointId:guid}` | `Tour.Update` | RemoveTourWaypoint |

### Task 4B — TourTourGuide (3 endpoints)

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 29 | GET | `/api/v1/tours/{id:guid}/guides` | Anonymous | ListTourGuides |
| 30 | POST | `/api/v1/tours/{id:guid}/guides` | `Tour.Update` | AssignTourGuide |
| 31 | DELETE | `/api/v1/tours/{id:guid}/guides/{guideUserId:guid}` | `Tour.Update` | UnassignTourGuide |

### Task 4C — ChildrenInfo (2 endpoints)

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 32 | GET | `/api/v1/tours/{id:guid}/children-info` | Anonymous | GetTourChildrenInfo |
| 33 | PUT | `/api/v1/tours/{id:guid}/children-info` | `Tour.Update` | UpdateTourChildrenInfo |

### Business Rules — TourWaypoint

#### B1. Domain Invariants

1. **Waypoints belong to a non-deleted Tour**: handler always verifies `tour != null && !tour.IsDeleted`. Suspended/Archived tours allow read but block writes (except admin).
2. **`SortOrder` is unique per `TourId`** and dense (no gaps): values 0, 1, 2, …, N-1 where N = waypoint count. This invariant is maintained by the handler on EVERY write operation (add/remove/reorder). Never trust the client to send correct SortOrder — handler recomputes.
3. **`WaypointType`** MUST be a proper enum in `ContentTours.Domain.Enums.WaypointType`:
   ```csharp
   public enum WaypointType : byte
   {
       Start = 0,      // exactly one per tour
       Stop = 1,       // any count
       Meal = 2,       // any count
       Photo = 3,      // any count
       Landmark = 4,
       RestStop = 5,
       End = 6         // exactly one per tour (if any waypoints exist)
   }
   ```
   Update `TourWaypoint.WaypointType` from `byte` to `WaypointType`. EF config stores as `HasConversion<byte>()`.
4. **Location invariants**: each waypoint has a `Location` value object. Lat [-90, 90], Lng [-180, 180], not (0,0). Non-Jordan coords log Warning but don't block.
5. **Name uniqueness within tour**: case-insensitive. Two waypoints on the same tour with name `"Treasury"` → `TourWaypoint.NameConflict` 409.
6. **DurationMinutes optional**: when set, `>= 0` (0 = drive-through, don't stop).
7. **Non-aggregate**: `TourWaypoint : BaseEntity`. No domain events, no soft-delete (hard delete), no `RowVersion`.

#### B2. Reorder Algorithm (the HARD part)

Endpoint: `PUT /api/v1/tours/{id}/waypoints/reorder`, body: `{ "waypointIds": ["guid1", "guid2", ...] }`.

**Preconditions**:
1. All ids in the list belong to this tour (set-equal check against `dbContext.TourWaypoints.Where(w => w.TourId == tourId).Select(w => w.Id)`).
2. No duplicate ids in the list.
3. List count equals the tour's current waypoint count (no adds/deletes during reorder — those are separate endpoints).
4. Caller is owner or admin.

**Algorithm** (runs in a serializable transaction):
```csharp
await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

var existing = await dbContext.TourWaypoints
    .Where(w => w.TourId == tourId)
    .ToListAsync(ct);  // loads with tracking

var existingIds = existing.Select(w => w.Id).ToHashSet();
var requestedIds = request.WaypointIds.ToHashSet();

if (!existingIds.SetEquals(requestedIds))
    return Result.Failure(new Error("TourWaypoint.ReorderSetMismatch",
        "Provided waypoint ids must equal the current set for this tour."), Outcome.Invalid);

if (request.WaypointIds.Count != request.WaypointIds.Distinct().Count())
    return Result.Failure(new Error("TourWaypoint.ReorderDuplicates", "..."), Outcome.Invalid);

var byId = existing.ToDictionary(w => w.Id);
for (int i = 0; i < request.WaypointIds.Count; i++)
    byId[request.WaypointIds[i]].SetSortOrder(i);   // protected domain method

await dbContext.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

**Concurrency tradeoff**: `TourWaypoint` doesn't have `RowVersion` in the current schema. Two parallel reorders could race. The serializable transaction protects against write-write corruption (second transaction gets serialization failure and is retried). This is acceptable for v1. **Document this tradeoff as a code comment** above the handler.

Postcondition: sort orders are 0..N-1 dense with no gaps. `TourWaypoint.SetSortOrder(int)` is a package-private method on the entity — validates `>= 0`.

#### B3. Add / Remove Semantics

- **AddTourWaypoint**: appends to the end. Handler reads `maxSortOrder + 1` under a serializable read (or uses a SQL sequence; for v1 a simple `.MaxAsync(w => (int?)w.SortOrder) ?? -1`). Assigns `SortOrder = maxSortOrder + 1`. Handler does NOT accept `SortOrder` from the request body — it's always appended.
- **RemoveTourWaypoint**: deletes the row, then re-compacts `SortOrder` for the remaining rows in the same transaction (decrement all rows with `SortOrder > deleted.SortOrder` by 1). This preserves invariant 2.
- Both add/remove must run in a serializable transaction because of the SortOrder invariant.

#### B4. Authorization Matrix (4A)

| Endpoint | Who | Permission |
|---|---|---|
| `GET /tours/{id}/waypoints` | Anonymous (always) | none |
| `POST /tours/{id}/waypoints` | Owner or admin | `ContentTours.Tour.Update` |
| `PUT /tours/{id}/waypoints/reorder` | Owner or admin | `ContentTours.Tour.Update` |
| `DELETE /tours/{id}/waypoints/{wid}` | Owner or admin | `ContentTours.Tour.Update` |

#### B5. Error Code Catalog (Task 4A)

| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | Parent missing |
| `Tour.NotOwner` | 403 | IDOR |
| `TourWaypoint.NotFound` | 404 | Waypoint id not under this tour |
| `TourWaypoint.NameConflict` | 409 | Case-insensitive duplicate name on same tour |
| `TourWaypoint.InvalidType` | 400 | `WaypointType` out of enum range |
| `TourWaypoint.InvalidLocation` | 400 | Bad coords |
| `TourWaypoint.ReorderSetMismatch` | 400 | Ids don't match current set |
| `TourWaypoint.ReorderDuplicates` | 400 | Duplicate id in reorder list |
| `TourWaypoint.ConcurrencyConflict` | 409 | Serializable tx retried and failed |

#### B6. Cache Policy (4A)

Tag: `tour-waypoints:{tourId}`, TTL 10 min. Invalidate on any mutation to any waypoint of that tour. Also bust `tour:{tourId}` (detail DTO embeds waypoints).

### Business Rules — TourTourGuide

#### B1. Junction Semantics

1. `TourTourGuide` is a pure junction: `(TourId, TourGuideId)` composite PK, `IsPrimary` bool. No Id column, no audit fields, no RowVersion. Stored at `content_tours.TourTourGuides`.
2. **`TourGuideId` is a User ID** — it's the UserId of someone with the `TourGuide` role in Security. NOT a separate entity in this module.
3. **Primary uniqueness**: at most one row per `TourId` has `IsPrimary = true`. A tour with any guide assignment MUST have exactly one primary. Tour with zero guides → zero primaries (obviously). This invariant is maintained by handlers.
4. **No duplicate assignment**: `(TourId, TourGuideId)` is unique — composite PK enforces. Trying to add a duplicate → `TourTourGuide.AlreadyAssigned` 409.

#### B2. Assign Logic

Endpoint: `POST /api/v1/tours/{id}/guides` with body `{ "tourGuideId": "guid", "isPrimary": true|false }`.

**Steps**:
1. Load parent tour; ownership check.
2. Call `await userRoleChecker.HasRoleAsync(request.TourGuideId, "TourGuide", ct)`.
   - Stub returns `true` and logs `Debug`. Add TODO to replace with real check once Security exposes `IUserRoleChecker`.
3. Check for existing assignment: if `(TourId, TourGuideId)` already exists → `TourTourGuide.AlreadyAssigned` 409.
4. If `request.IsPrimary == true`:
   - Demote existing primary (if any): `dbContext.TourTourGuides.Where(x => x.TourId == tourId && x.IsPrimary).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct);`
5. If `request.IsPrimary == false` AND this is the FIRST assignment (no existing rows) → promote to primary automatically (invariant 3).
6. Insert the new row.
7. Write `TourGuideAssignedIntegrationEvent` to outbox (see below).
8. Save + cache invalidate.

#### B3. Unassign Logic

Endpoint: `DELETE /api/v1/tours/{id}/guides/{guideUserId}`.

**Steps**:
1. Load parent; ownership check.
2. Load the assignment. 404 if missing.
3. If it's the primary AND other assignments exist: promote the OLDEST remaining by `CreatedAt` ASC to primary (invariant 3).
   - Since the junction has no `CreatedAt`, add one in migration `AddTourTourGuideAuditFields` — piggy-back onto Task 4A migration or a dedicated one.
   - Alternative without migration: promote the one with the smallest `TourGuideId` (deterministic, no schema change needed). **This sprint uses the deterministic-smallest-id rule** and documents why in code.
4. Delete the assignment.
5. Write `TourGuideUnassignedIntegrationEvent`.
6. Save + cache invalidate.

#### B4. List — Public

- Endpoint: `GET /api/v1/tours/{id}/guides` — **anonymous**, unlike ContentPlaces BusinessStaff (which is private). Tour guides are part of the public product surface.
- Response: `{ tourGuideId, displayName, avatarUrl, isPrimary, assignedAt }[]`.
- **No PII** — email, phone, address are NOT exposed. `displayName` and `avatarUrl` come from Accounts via a cross-module view `IProfileLookupService.GetPublicProfileAsync(userId, ct)`. For v1, if the service isn't wired, fall back to `displayName = tourGuideId.ToString()` and `avatarUrl = null`, log Debug.
- Sort: primary first, then by `TourGuideId` ASC.
- Cache: tag `tour-guides:{tourId}`, TTL 10 min.

#### B5. Error Code Catalog (Task 4B)

| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | Parent missing |
| `Tour.NotOwner` | 403 | IDOR |
| `TourTourGuide.NotFound` | 404 | No assignment for `(tourId, guideUserId)` |
| `TourTourGuide.UserIsNotGuide` | 400 | UserRoleChecker returned false (user lacks TourGuide role) |
| `TourTourGuide.AlreadyAssigned` | 409 | Duplicate insert attempted |
| `TourTourGuide.UserNotFound` | 400 | UserId doesn't exist in Security — stub accepts all for v1 |

### Business Rules — ChildrenInfo

#### B1. Storage on Tour Aggregate

This is **NOT a separate entity**. It's a set of fields on `Tour` updated through a dedicated domain method. Total child-relevant state:

| Field | Type | Existing? | Purpose |
|---|---|---|---|
| `IsChildFriendly` | `bool` | ✅ already on Tour | marketing flag — shown in search facet |
| `AgeRestriction` | `int?` | ✅ already on Tour | minimum age required (if any); null = no restriction. Used for bookings validation |
| `MinChildAge` | `int?` | ❌ ADD via migration | minimum child age accepted (e.g., `3` → tour accepts children ≥ 3yo) |
| `MaxChildAge` | `int?` | ❌ ADD via migration | maximum child age counted as "child" for pricing purposes |
| `ChildFacilities` | `string?` (max 500) | ❌ ADD via migration | comma-separated facility codes: `"Stroller,HighChair,ChangingStation,ChildMenu,NursingRoom"` |

Migration: `AddTourChildrenInfoFields`. On critical path for Ezz — generate AND run locally before endpoint work. Fields are nullable so backfill is safe.

#### B2. Domain Method

Add to `Tour` aggregate (Task 1 added the shell; Ezz fills this one):
```csharp
public void UpdateChildrenInfo(
    bool isChildFriendly,
    int? ageRestriction,
    int? minChildAge,
    int? maxChildAge,
    string? childFacilities)
{
    if (minChildAge.HasValue && (minChildAge < 0 || minChildAge > 18))
        throw new ArgumentOutOfRangeException(nameof(minChildAge));
    if (maxChildAge.HasValue && (maxChildAge < 0 || maxChildAge > 18))
        throw new ArgumentOutOfRangeException(nameof(maxChildAge));
    if (minChildAge.HasValue && maxChildAge.HasValue && maxChildAge.Value <= minChildAge.Value)
        throw new ArgumentException("MaxChildAge must be greater than MinChildAge.");
    if (ageRestriction.HasValue && (ageRestriction < 0 || ageRestriction > 120))
        throw new ArgumentOutOfRangeException(nameof(ageRestriction));

    IsChildFriendly  = isChildFriendly;
    AgeRestriction   = ageRestriction;
    MinChildAge      = minChildAge;
    MaxChildAge      = maxChildAge;
    ChildFacilities  = childFacilities?.Trim();
    MarkUpdated();
    AddDomainEvent(new TourUpdatedDomainEvent(Id, Name, Description, PlaceId,
        NameChanged: false, DescriptionChanged: false,
        ShortDescriptionChanged: false, PlaceIdChanged: false,
        ChildrenInfoChanged: true));   // NEW flag to drive SEO re-index
}
```

#### B3. Facility Taxonomy (controlled vocabulary)

`ChildFacilities` is a CSV of codes from an enum-like list. Task 4 ships the list in `ContentTours.Domain.Enums.ChildFacility`:
```csharp
public enum ChildFacility : byte
{
    Stroller           = 0,
    HighChair          = 1,
    ChangingStation    = 2,
    ChildMenu          = 3,
    NursingRoom        = 4,
    ChildToilet        = 5,
    PlayArea           = 6,
    ChildSeat          = 7,
    BabyCarrier        = 8,
    AirConditioning    = 9,
}
```

The `ChildFacilities` string is validated by parsing each CSV token into this enum. Unknown token → `Tour.UnknownChildFacility` 400. Ezz writes a tiny `ChildFacilitiesParser` helper in the validator assembly.

#### B4. Authorization Matrix

| Endpoint | Who | Permission |
|---|---|---|
| `GET /tours/{id}/children-info` | Anonymous | none |
| `PUT /tours/{id}/children-info` | Owner or admin | `ContentTours.Tour.Update` |

#### B5. Error Code Catalog (Task 4C)

| Code | HTTP | When |
|---|---|---|
| `Tour.NotFound` | 404 | Parent missing / soft-deleted |
| `Tour.NotOwner` | 403 | IDOR |
| `Tour.InvalidChildrenAgeRange` | 400 | Max ≤ Min |
| `Tour.InvalidAgeBound` | 400 | Age outside 0..18 (child ages) or 0..120 (ageRestriction) |
| `Tour.UnknownChildFacility` | 400 | CSV token not in enum |
| `Tour.ChildFacilitiesTooLong` | 400 | String >500 chars |

#### B6. Acceptance Test Scenarios

1. GET unknown tour → 404.
2. PUT as non-owner non-admin → 403.
3. PUT with `minChildAge=5, maxChildAge=3` → 400 `Tour.InvalidChildrenAgeRange`.
4. PUT with `childFacilities="HighChair,UnknownThing"` → 400 `Tour.UnknownChildFacility`.
5. PUT valid → 204; GET reflects new values; `TourUpdatedIntegrationEvent` fired with `ChildrenInfoChanged=true`.
6. Setting `IsChildFriendly=true` with all age fields null is valid (means "yes for any age").

### Domain & Integration Event Handler Logic (Task 4)

#### Waypoints — no domain events

`TourWaypoint` is a non-aggregate. Neither add/remove nor reorder raises a domain event. If Analytics wants to know about waypoint changes, the handler writes an integration event directly:

```csharp
// In AddTourWaypointCommandHandler, BEFORE SaveChangesAsync:
var integrationEvent = new TourWaypointChangedIntegrationEvent(
    TourId: tour.Id,
    WaypointId: waypoint.Id,
    ChangeType: "Added");
dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
```

**Default for this sprint**: DO NOT ship this event unless Analytics owner explicitly requests it. Skip Task 4A outbox writes.

#### Guides — integration events only (junction, no domain events)

Two events, written directly from the `Assign` / `Unassign` handlers before save:

```csharp
// ContentTours.Contracts/IntegrationEvents/

/// <summary>
/// Raised when a Tour Guide is assigned to a Tour.
/// Consumers: Messaging (notify guide they were assigned), Analytics (guide activity).
/// </summary>
public sealed record TourGuideAssignedIntegrationEvent(
    Guid TourId,
    Guid TourGuideUserId,
    bool IsPrimary,
    Guid AssignedByUserId) : IntegrationEventBase;

/// <summary>
/// Raised when a Tour Guide is unassigned from a Tour.
/// Consumers: Messaging (notify guide), Booking (block future bookings that depended on this guide — future feature).
/// </summary>
public sealed record TourGuideUnassignedIntegrationEvent(
    Guid TourId,
    Guid TourGuideUserId,
    Guid UnassignedByUserId) : IntegrationEventBase;
```

Handler pattern (Assign):
```csharp
public async Task<Result> Handle(AssignTourGuideCommand cmd, CancellationToken ct)
{
    var tour = await tourRepository.GetByIdAsync(cmd.TourId, ct);
    if (tour is null || tour.IsDeleted)
        return Result.Failure(Error.NotFound("Tour.NotFound"), Outcome.NotFound);

    if (tour.CreatedByUserId != currentUser.UserId && !currentUser.IsInRole("Admin"))
        return Result.Failure(Error.Forbidden("Tour.NotOwner"), Outcome.Forbidden);

    var isGuide = await userRoleChecker.HasRoleAsync(cmd.TourGuideUserId, "TourGuide", ct);
    if (!isGuide)
        return Result.Failure(Error.Invalid("TourTourGuide.UserIsNotGuide"), Outcome.Invalid);

    var alreadyAssigned = await dbContext.TourTourGuides
        .AnyAsync(x => x.TourId == cmd.TourId && x.TourGuideId == cmd.TourGuideUserId, ct);
    if (alreadyAssigned)
        return Result.Failure(Error.Conflict("TourTourGuide.AlreadyAssigned"), Outcome.Conflict);

    // Count existing + determine effective IsPrimary
    var existingCount = await dbContext.TourTourGuides
        .CountAsync(x => x.TourId == cmd.TourId, ct);
    var effectivePrimary = cmd.IsPrimary || existingCount == 0;

    // Demote existing primary if we're claiming it
    if (effectivePrimary && existingCount > 0)
    {
        await dbContext.TourTourGuides
            .Where(x => x.TourId == cmd.TourId && x.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct);
    }

    var assignment = TourTourGuide.Create(cmd.TourId, cmd.TourGuideUserId, effectivePrimary);
    dbContext.TourTourGuides.Add(assignment);

    // Outbox write — before save, committed atomically
    var integrationEvent = new TourGuideAssignedIntegrationEvent(
        cmd.TourId, cmd.TourGuideUserId, effectivePrimary, currentUser.UserId!.Value);
    dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

    await unitOfWork.SaveChangesAsync(ct);
    await cache.RemoveByTagAsync($"tour-guides:{cmd.TourId}", ct);
    await cache.RemoveByTagAsync($"tour:{cmd.TourId}", ct);

    logger.LogInformation("Assigned guide {UserId} to tour {TourId}; primary={IsPrimary}",
        cmd.TourGuideUserId, cmd.TourId, effectivePrimary);
    return Result.Success();
}
```

#### ChildrenInfo — reuses `TourUpdatedDomainEvent`

Since children-info is on the Tour aggregate, `UpdateTourChildrenInfo` raises the **same** `TourUpdatedDomainEvent` already owned by Mahmoud (Task 1). The event record gets a new field `ChildrenInfoChanged: bool` so consumers can detect children-specific changes:

```csharp
// ContentTours.Domain/Events/TourUpdatedDomainEvent.cs (Mahmoud owns file; Ezz adds the flag)
public sealed record TourUpdatedDomainEvent(
    Guid TourId,
    string Name,
    string? Description,
    Guid? PlaceId,
    bool NameChanged,
    bool DescriptionChanged,
    bool ShortDescriptionChanged,
    bool PlaceIdChanged,
    bool ChildrenInfoChanged = false) : DomainEventBase;
```

`TourUpdatedDomainEventHandler` (owned by Mahmoud) detects `ChildrenInfoChanged` and:
- Writes `TourUpdatedIntegrationEvent` with a `ChildrenInfoChanged` mirror flag.
- ContentSeo consumer later uses the flag to re-index child-friendly filters in the sitemap.

Registry keys added by Task 4:
```
content-tours.tour-guide.assigned.v1
content-tours.tour-guide.unassigned.v1
content-tours.waypoint.changed.v1   (only if shipped — default NO)
```

### WBS — Task 4 (36 hrs)

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 4.1 | Study Task 1's `Tour` entity + Tech Lead walkthrough + set up IDE. | 3 | W 2026-05-06 18:00 |
| 4.2 | Add `WaypointType` enum, `Tour.UpdateChildrenInfo` method, migration `AddTourChildrenInfoFields` | 4 | R 2026-05-07 15:00 |
| 4.3 | `ListTourWaypoints` + `AddTourWaypoint` | 4 | S 2026-05-10 13:00 |
| 4.4 | `ReorderTourWaypoints` (batch rewrite) + `RemoveTourWaypoint` | 5 | S 2026-05-10 18:00 |
| 4.5 | `ListTourGuides` + `AssignTourGuide` (primary-swap logic) | 5 | M 2026-05-11 15:00 |
| 4.6 | `UnassignTourGuide` (promote-oldest-on-primary-removal logic) | 4 | T 2026-05-12 12:00 |
| 4.7 | `GetTourChildrenInfo` + `UpdateTourChildrenInfo` | 3 | T 2026-05-12 17:00 |
| 4.8 | Presentation: 9 endpoints + PR | 5 | R 2026-05-14 17:00 |
| 4.9 | Fix review comments + re-run build/tests | 3 | M 2026-05-18 17:00 |

### Validator Rules

```
-- AddTourWaypoint --
Name:               .NotEmpty().MaximumLength(200)
Description:        .MaximumLength(1000).When(x => x.Description != null)
Location.Latitude:  .InclusiveBetween(-90, 90)
Location.Longitude: .InclusiveBetween(-180, 180)
SortOrder:          .GreaterThanOrEqualTo(0)
DurationMinutes:    .GreaterThanOrEqualTo(0).When(x => x.DurationMinutes.HasValue)
WaypointType:       .IsInEnum()

-- ReorderTourWaypoints --
WaypointIds:        .NotEmpty() — custom rule: count == tour's waypoint count, no duplicates, every id belongs to this tour

-- AssignTourGuide --
TourGuideId:        .NotEqual(Guid.Empty)
-- also custom: no duplicate assignment for same (TourId, TourGuideId)

-- UpdateTourChildrenInfo --
IsChildFriendly:    (boolean, no rule)
MinChildAge:        .InclusiveBetween(0, 18).When(x => x.MinChildAge.HasValue)
MaxChildAge:        .InclusiveBetween(0, 18).GreaterThan(x => x.MinChildAge).When(x => x.MaxChildAge.HasValue)
ChildFacilities:    .MaximumLength(500).When(x => x.ChildFacilities != null)
```

---

## Task 5 — Fadwa · TourPackage + TourPackageInclusion

**6 endpoints · 36 hours · Deadline: Wed 2026-05-20 · 17:00**
**Depends on: Task 1 (Tours must exist to bundle). Earliest start: W 2026-05-06 afternoon.**

### 🎯 Entities Touched

| Entity | Base Class | Role | Fadwa's Responsibility |
|---|---|---|---|
| **`TourPackage`** | `AuditableEntity` (current) — **possibly promoted to `IAggregateRoot`** in WBS 5.1 (Tech Lead decision, see B0) | **Primary entity** of this task — a bundle of ≥2 tours | Own the full lifecycle. Add domain methods: `Create(name, description?, price, currency, validFrom?, validTo?, maxParticipants?, createdByUserId, includedTourIds)`, `Update(...)`, `SoftDelete()`, `AddInclusion(description, sortOrder)`. Handler enforces: (1) **≥2 distinct tours** invariant, (2) **currency match** with every included tour, (3) **all tours are Approved + not deleted**, (4) **all tours owned by caller** (unless Admin — cross-owner allowed), (5) **atomic capacity check** via `ITourCapacityService` stub, (6) **validity window** (`ValidFrom` immutable post-create, `ValidTo > UtcNow`), (7) `MaxParticipants ≤ min(tours' MaxGroupSize)`. Has `RowVersion` (AuditableEntity). Schema migration `AddTourPackageAudit` adds `CreatedByUserId Guid` field. |
| **`TourPackageInclusion`** | `BaseEntity` | **Non-aggregate junction/child** of `TourPackage` | Own the full lifecycle. Add domain method: `Create(tourPackageId, description, sortOrder)`. Handler enforces: (1) **composite uniqueness** `(TourPackageId, Description)` case-insensitive, (2) **auto-assigned `SortOrder`** (`existingMax + 1`, never trust client), (3) hard delete only, no `RowVersion`. These are **marketing bullet points** (e.g., "Jerash Old City"), NOT the tour-bundle list — the bundle is tracked separately via `TourPackage.IncludedTourIds` (a collection/JSON column or a separate junction to be clarified in WBS 5.2). |
| `Tour` | `AuditableEntity, IAggregateRoot` (owned by Task 1) | **Read-only** from Fadwa's perspective | Handler loads included tours in bulk to run validation (currency match, approved status, ownership, `MaxGroupSize` cap). Fadwa does NOT mutate `Tour`. |
| `ITourCapacityService` (new abstraction) | Interface in `ContentTours.Application/Interfaces/` | Cross-module capacity check hook | Fadwa ships `NoOpTourCapacityService` stub in Infrastructure returning `AllHaveCapacity = true`. Booking module later replaces the DI registration with a real implementation. Called from `CreateTourPackage` and `AddPackageInclusion` handlers. |

> **Aggregate-marker decision (WBS 5.1)**: `TourPackage` is currently `AuditableEntity` (non-aggregate). Two options:
> - **Option A — Promote to `IAggregateRoot`**: clean event dispatch via UoW, proper encapsulation, Inclusions raise events via parent. Requires Tech Lead approval + migration PR at start of Task 5. **Preferred.**
> - **Option B — Keep as `AuditableEntity`**: handlers write outbox directly (same pattern as `TourSchedule`/`TourPricingTier`). Zero schema risk. Fallback if Option A is rejected.
>
> The business rules below apply IDENTICALLY regardless of choice — only the handler wiring differs (domain event handler in Option A vs direct outbox write in Option B).
>
> **`TourPackageInclusion` is always a non-aggregate.** Even if `TourPackage` becomes an aggregate, inclusions raise no events on their own — they're child entities of the package aggregate.

### Endpoints

| # | Method | Route | Auth | Handler |
|---|--------|-------|------|---------|
| 34 | GET | `/api/v1/tours/packages` | Anonymous | ListTourPackages |
| 35 | GET | `/api/v1/tours/packages/{id:guid}` | Anonymous | GetTourPackageById |
| 36 | POST | `/api/v1/tours/packages` | `Package.Create` | CreateTourPackage |
| 37 | PUT | `/api/v1/tours/packages/{id:guid}` | `Package.Update` | UpdateTourPackage |
| 38 | DELETE | `/api/v1/tours/packages/{id:guid}` | `Package.Delete` | DeleteTourPackage (soft) |
| 39 | POST | `/api/v1/tours/packages/{id:guid}/inclusions` | `Package.Update` | AddPackageInclusion |

### Business Rules — TourPackage

#### B0. Aggregate Decision (resolve in WBS 5.1 with Tech Lead)

The current schema has `TourPackage : AuditableEntity`. For this sprint you have two options:

| Option | Pros | Cons | Decision |
|---|---|---|---|
| **A. Promote to `IAggregateRoot`** | Domain events work via UoW; clean aggregate encapsulation; Inclusions raise events via parent | Requires schema confirmation, PR review + testing | **Preferred if Tech Lead approves** |
| **B. Keep as AuditableEntity** | Zero schema risk | Handlers write outbox directly; no domain events; feels like "non-aggregate" but functions similarly | Fallback |

Either way, the business rules below apply identically. The only difference is WHERE the outbox write happens (domain event handler vs. command handler).

#### B1. Domain Invariants

1. **A package is a bundle of ≥2 distinct tours**: `Inclusions` must contain at least 2 unique `TourId`s. One-tour "packages" are forbidden by definition — use `Tour` alone for that. Enforced in `TourPackage.Create(...)` → `ArgumentException` if violated.
2. **All included tours share one currency**: `TourPackage.Currency` must match every `Tour.Currency` of every inclusion. A JOD package cannot include a USD tour. Enforced BOTH in the handler (Result.Failure) AND in `Create(...)` as a defensive invariant.
3. **Provider owns every included tour** (unless caller is Admin): handler enforces `tours.All(t => t.CreatedByUserId == currentUser.UserId)`. Cross-owner bundles require Admin. Failure → `TourPackage.NotOwnerOfAllTours` 403.
4. **Every included tour must be Approved**: `tours.All(t => t.Status == TourStatus.Approved && !t.IsDeleted)`. Draft / Pending / Rejected / Suspended tours can't be in a public package. Failure → `TourPackage.IncludesNonApprovedTour` 422.
5. **Atomic capacity check** (B3 below).
6. **Validity window**: `ValidFrom < ValidTo` (when both set); `ValidTo > UtcNow` on create (can't create an expired-at-birth package).
7. **Package price is not computed** from included tours in v1: provider sets `TourPackage.Price` directly. Finance module later compares it to `SUM(t.BasePrice)` to derive a `PackageDiscountPercent` for discount displays.
8. **MaxParticipants** is optional. Null = inherit the MIN of included tours' `MaxGroupSize`. Set = explicit cap (must be ≤ every included tour's `MaxGroupSize`).
9. **Soft delete** preserves inclusions — they're historical. Deleted package is hidden from public list and booking cannot book it, but ID remains queryable for historical receipts.

#### B2. Authorization Matrix

| Endpoint | Who | Permission |
|---|---|---|
| `GET /tours/packages` | Anonymous. Returns `!IsDeleted && IsActive && ValidTo > UtcNow OR ValidTo null` | none |
| `GET /tours/packages/{id}` | Anonymous. Returns any (even deleted) for owner/admin; only public-facing for anon | none |
| `POST /tours/packages` | Provider (must own all tours) or Admin (can cross-own) | `ContentTours.Package.Create` |
| `PUT /tours/packages/{id}` | Owner of the package (creator) or Admin | `ContentTours.Package.Update` |
| `DELETE /tours/packages/{id}` | Owner or Admin | `ContentTours.Package.Delete` |
| `POST /tours/packages/{id}/inclusions` | Owner or Admin | `ContentTours.Package.Update` |

**Owner of a package** = the user whose `CreatedByUserId` matches the first inclusion's `Tour.CreatedByUserId` at create-time. Because a package inherits ownership from its tours, store a `CreatedByUserId` field on `TourPackage` (add via migration `AddTourPackageAudit` in WBS 5.2). Cross-owner packages created by Admin have `CreatedByUserId = Admin's UserId`.

#### B3. Atomic Capacity Check Algorithm

Runs on `CreateTourPackage` AND `AddPackageInclusion`. The goal: ensure every included tour has enough slot capacity for the package's `MaxParticipants` value.

```csharp
// Interface lives in ContentTours.Application/Interfaces/ITourCapacityService.cs
// Stub implementation in Infrastructure returns "true" until Booking module ships the real one.
public interface ITourCapacityService
{
    Task<CapacityCheckResult> CheckCapacityAsync(
        IReadOnlyList<Guid> tourIds,
        int requiredParticipants,
        CancellationToken ct);
}

public sealed record CapacityCheckResult(
    bool AllHaveCapacity,
    IReadOnlyList<TourCapacityIssue> Issues);

public sealed record TourCapacityIssue(
    Guid TourId,
    int RequestedParticipants,
    int? AvailableParticipants);  // null if booking module not yet live
```

Handler logic in `CreateTourPackage`:
```csharp
var required = cmd.MaxParticipants ?? 1;  // default to 1 if not specified
var capacity = await tourCapacityService.CheckCapacityAsync(
    cmd.IncludedTourIds, required, ct);
if (!capacity.AllHaveCapacity)
    return Result.Failure(
        new Error("TourPackage.AtomicBookingFailed",
            $"{capacity.Issues.Count} included tours cannot guarantee {required} participants."),
        Outcome.Conflict,
        context: capacity.Issues);   // attach list for UI
```

Stub implementation (until Booking module):
```csharp
internal sealed class NoOpTourCapacityService(
    ILogger<NoOpTourCapacityService> logger) : ITourCapacityService
{
    public Task<CapacityCheckResult> CheckCapacityAsync(
        IReadOnlyList<Guid> tourIds, int required, CancellationToken ct)
    {
        logger.LogDebug("Capacity check stub invoked for {Count} tours, required={Required}",
            tourIds.Count, required);
        return Task.FromResult(new CapacityCheckResult(true, []));
    }
}
```

When Booking ships, it registers a real implementation that queries remaining slot capacity per tour. ContentTours doesn't change.

#### B4. Pricing Consistency Check

On Create / Update / AddInclusion:
- Load all included tours.
- Assert `tours.All(t => t.Currency.Equals(package.Currency, StringComparison.OrdinalIgnoreCase))`.
- If any mismatch → `TourPackage.CurrencyMismatch` 400 with offending tour ids in payload.

Note: if a tour's currency changes AFTER a package is created (extremely rare), the package is not auto-fixed. Provider must recreate the package. Future sprint may add an integration event `TourCurrencyChangedIntegrationEvent` that triggers a package-revalidation background job.

#### B5. Validity Window

- `ValidFrom` null = "valid immediately". `ValidTo` null = "valid forever".
- On create: if `ValidFrom.HasValue && ValidFrom < UtcNow` → `TourPackage.InvalidValidityWindow` 400 (can't set start in past).
- On create: if `ValidTo.HasValue && ValidTo <= UtcNow` → same error.
- On update: `ValidFrom` is immutable post-creation (protects customers mid-booking). Only `ValidTo` can be extended / shortened. Shortening below `UtcNow` → validity expires immediately.
- List endpoint filters out packages where `ValidTo < UtcNow`.

#### B6. List / Detail Semantics

- List (`GET /tours/packages`): paginated, max pageSize 50. Filters: `providerId`, `minPrice`, `maxPrice`, `currency`, `includeTourId` (matches any inclusion), `validOnDate` (default `UtcNow`).
- Detail (`GET /tours/packages/{id}`): includes all inclusions, each resolved into a `TourSummaryDto`. Soft-deleted packages return 404 for anonymous; admin sees `IsDeleted=true` flag.
- Sort: `newest` (default) | `price_asc` | `price_desc` | `validity_ending_soon` (ValidTo ASC, NULLs LAST).

#### B7. Error Code Catalog (Task 5)

| Code | HTTP | When |
|---|---|---|
| `TourPackage.NotFound` | 404 | Package missing / soft-deleted (non-admin) |
| `TourPackage.NotOwner` | 403 | Non-owner non-admin |
| `TourPackage.InsufficientInclusions` | 400 | Fewer than 2 distinct `TourId`s in inclusion list |
| `TourPackage.DuplicateInclusions` | 400 | Same `TourId` appears twice |
| `TourPackage.IncludesUnknownTour` | 400 | `TourId` doesn't exist |
| `TourPackage.IncludesDeletedTour` | 400 | `TourId` is soft-deleted |
| `TourPackage.IncludesNonApprovedTour` | 422 | At least one included tour is not `Status == Approved` |
| `TourPackage.NotOwnerOfAllTours` | 403 | Provider trying to bundle another provider's tour |
| `TourPackage.CurrencyMismatch` | 400 | Package currency ≠ every included tour's currency |
| `TourPackage.MaxParticipantsExceedsTour` | 400 | Package max > min(tours' maxGroupSize) |
| `TourPackage.AtomicBookingFailed` | 409 | Capacity check returned not-all-have-capacity |
| `TourPackage.InvalidValidityWindow` | 400 | ValidFrom/To violates B5 |
| `TourPackage.ValidFromImmutable` | 409 | Update tried to change `ValidFrom` |
| `TourPackage.ConcurrencyConflict` | 409 | RowVersion mismatch on Update/Delete |
| `TourPackageInclusion.Duplicate` | 409 | Same Description on same package |
| `TourPackageInclusion.NotFound` | 404 | Inclusion id not under this package |

#### B8. Cache Policy

| Query | Key | TTL | Tags |
|---|---|---|---|
| `ListTourPackages` | `ct:packages:p{page}:s{pageSize}:prov:{providerId}:...` | 5 min | `packages`, `packages:list` |
| `GetTourPackageById` | `ct:package:{id}:lang:{Accept-Language}` | 5 min | `packages`, `package:{id}` |

**Invalidation**:
- Create package → `packages:list`, and for every included tour: `tour:{tourId}` (detail DTO may mention packages).
- Update / Delete / AddInclusion → `package:{id}`, `packages:list`, `tour:{includedTourId}` (each).
- When Finance later publishes `DiscountAppliedIntegrationEvent` targeting a package, that consumer busts `package:{id}`.

#### B9. Acceptance Test Scenarios

1. Create package with 1 inclusion → 400 `TourPackage.InsufficientInclusions`.
2. Create package with 2 inclusions of same TourId → 400 `TourPackage.DuplicateInclusions`.
3. Create package with a Draft tour → 422 `TourPackage.IncludesNonApprovedTour`.
4. Create package where one tour is owned by another provider (caller not Admin) → 403.
5. Create package with JOD currency including a USD tour → 400 `TourPackage.CurrencyMismatch`.
6. Create package with `MaxParticipants=50` where one tour's `MaxGroupSize=20` → 400.
7. Create package with all checks green → 201; `TourPackageCreatedIntegrationEvent` in outbox.
8. Update package: change `ValidFrom` → 409 `TourPackage.ValidFromImmutable`.
9. Update package: extend `ValidTo` by 30 days → 204.
10. Delete package → soft-deleted; list excludes it; GET by id returns 404 for anonymous; `TourPackageDeletedIntegrationEvent` in outbox.
11. Add inclusion making a 2-tour package a 3-tour package → capacity re-checked; 201 if green.
12. Two admins concurrently update same package → first wins, second gets 409 `TourPackage.ConcurrencyConflict`.

### Business Rules — TourPackageInclusion

#### B1. Junction Semantics

1. `TourPackageInclusion : BaseEntity` — non-aggregate. No domain events (events live on parent `TourPackage`). No soft-delete (hard delete only). No RowVersion.
2. **Composite uniqueness per package**: the pair `(TourPackageId, Description)` is unique (case-insensitive). Adding same description twice → 409 `TourPackageInclusion.Duplicate`.
3. **Referential link** to the tour: an Inclusion carries a `Description` today (e.g., `"Jerash Old City"`) but will gain `IncludedTourId` in a future sprint once we move to structured inclusions. For this sprint, `Description` is free text, max 500 chars. The multi-tour bundling list in `CreateTourPackage.IncludedTourIds` is a SEPARATE concept — it's the set of tours this package references. Inclusions are the bullet-point marketing list ("what's included").
4. **SortOrder auto-assigned**: `existingMax + 1`. Never trust client-supplied SortOrder. No reorder endpoint in this sprint — future enhancement.

#### B2. Direct Inclusion Endpoint Logic

Endpoint `POST /tours/packages/{id}/inclusions` exists as a **convenience** for providers to append marketing bullet points after package creation. Alternative: POST to the main package endpoint with a full new inclusion list (replace semantics) — not implemented this sprint.

Handler steps:
1. Load package, ownership check, soft-delete check.
2. Check duplicate Description.
3. Compute `nextSortOrder = (max existing) + 1`.
4. Add inclusion.
5. Write `TourPackageInclusionAddedIntegrationEvent` to outbox (optional — skip unless needed by Analytics).
6. Save + cache invalidate `package:{id}`.

Does NOT re-run capacity check (capacity is tied to `IncludedTourIds`, which is set at package creation and edited via `UpdateTourPackage`, not via this inclusion endpoint).

#### B3. Error Code Catalog

See B7 above.

### Domain & Integration Event Handler Logic (Task 5)

Assuming **Option A** (aggregate-root) from B0 is approved.

#### `TourPackageCreatedDomainEventHandler`

Responsibilities:
- Write `TourPackageCreatedIntegrationEvent` to outbox.
- Trigger package-level translation via `IEntityTranslationOrchestrator` (Ar ↔ En on `Name`, `Description`).
- Log structured info.

```csharp
public sealed class TourPackageCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ContentToursDbContext dbContext,
    ILogger<TourPackageCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourPackageCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourPackageCreatedDomainEvent> note, CancellationToken ct)
    {
        var evt = note.DomainEvent;

        // Non-blocking translation
        try { await orchestrator.TranslateEntityAsync("TourPackage", evt.PackageId,
                new[] { "Name", "Description" }, sourceLanguageCode: "en", ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { logger.LogWarning(ex, "Package translation failed for {PackageId}", evt.PackageId); }

        var integrationEvent = new TourPackageCreatedIntegrationEvent(
            PackageId: evt.PackageId,
            Name: evt.Name,
            Price: evt.Price,
            Currency: evt.Currency,
            IncludedTourIds: evt.IncludedTourIds,
            CreatedByUserId: evt.CreatedByUserId);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        logger.LogInformation("Queued TourPackageCreatedIntegrationEvent for {PackageId}", evt.PackageId);
    }
}
```

#### `TourPackageUpdatedDomainEventHandler`

Same shape as the Tour version — diff-flag-aware, re-translates only on name/description change.

#### Delete: `DeleteTourPackageCommandHandler` (no domain event, handler writes integration event directly)

```csharp
// Inside DeleteTourPackageCommandHandler, after successful state change, before SaveChanges:
var integrationEvent = new TourPackageDeletedIntegrationEvent(
    PackageId: cmd.Id,
    IncludedTourIds: package.IncludedTourIds,
    DeletedByUserId: currentUser.UserId!.Value);
dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
```

Reason no domain event: in this codebase, soft-delete on `AuditableEntity` is a protected setter operation (via `SoftDelete()` inherited method) that doesn't currently raise events. Adding one is a cross-cutting change — out of scope for this sprint.

#### Integration events emitted by Task 5

```csharp
// ContentTours.Contracts/IntegrationEvents/

/// <summary>
/// Raised when a TourPackage is created.
/// Consumers: Finance (evaluate package discount eligibility), Messaging (notify provider),
///            Analytics (track bundle creation), ContentSeo (add to sitemap).
/// </summary>
public sealed record TourPackageCreatedIntegrationEvent(
    Guid PackageId,
    string Name,
    decimal Price,
    string Currency,
    IReadOnlyList<Guid> IncludedTourIds,
    Guid CreatedByUserId) : IntegrationEventBase;

/// <summary>
/// Raised when a TourPackage identity-bearing field or validity window changes.
/// Consumers: Finance (recompute discount), ContentSeo (refresh sitemap entry).
/// </summary>
public sealed record TourPackageUpdatedIntegrationEvent(
    Guid PackageId,
    string Name,
    decimal Price,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool NameChanged,
    bool PriceChanged,
    bool ValidityChanged) : IntegrationEventBase;

/// <summary>
/// Raised on soft-delete.
/// Consumers: Booking (block new bookings on this package), ContentSeo (remove from sitemap),
///            Finance (terminate any active discounts attached to package).
/// </summary>
public sealed record TourPackageDeletedIntegrationEvent(
    Guid PackageId,
    IReadOnlyList<Guid> IncludedTourIds,
    Guid DeletedByUserId) : IntegrationEventBase;

/// <summary>
/// Optional — raised on addition of a marketing inclusion bullet.
/// Consumers: Analytics (content richness). SKIP unless requested.
/// </summary>
public sealed record TourPackageInclusionAddedIntegrationEvent(
    Guid PackageId,
    Guid InclusionId,
    string Description) : IntegrationEventBase;
```

Registry keys:
```
content-tours.package.created.v1
content-tours.package.updated.v1
content-tours.package.deleted.v1
content-tours.package-inclusion.added.v1   (optional)
```

### WBS — Task 5 (36 hrs)

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 5.1 | Domain walkthrough with Tech Lead — confirm `TourPackage` aggregate treatment + domain events | 2 | W 2026-05-06 18:00 |
| 5.2 | Domain: `TourPackage.Create/Update/SoftDelete/AddInclusion`, `TourPackageInclusion.Create`, `TourPackageCreatedDomainEvent`, `TourPackageUpdatedDomainEvent`, `TourPackageDeletedIntegrationEvent`. Migration if aggregate marker added. | 5 | R 2026-05-07 18:00 |
| 5.3 | Application: `ListTourPackages` (paginated, filtered) + `GetTourPackageById` with full inclusions projection | 4 | S 2026-05-10 15:00 |
| 5.4 | Application: `CreateTourPackage` (the HARD one — multi-tour validation, capacity check, cross-owner check, currency check) | 8 | T 2026-05-12 18:00 |
| 5.5 | Application: `UpdateTourPackage` + `DeleteTourPackage` | 4 | W 2026-05-13 17:00 |
| 5.6 | Application: `AddPackageInclusion` with capacity re-check | 4 | R 2026-05-14 17:00 |
| 5.7 | Infrastructure: DI wiring, outbox writes for created/updated/deleted integration events, cache tags (`packages`, `package:{id}`) | 3 | S 2026-05-17 15:00 |
| 5.8 | Presentation: 6 endpoints | 3 | M 2026-05-18 17:00 |
| 5.9 | Fix review comments + re-run build/tests + PR | 3 | W 2026-05-20 17:00 |

### Validator Rules

```
-- CreateTourPackage --
Name:              .NotEmpty().MaximumLength(200)
Description:       .MaximumLength(1000).When(x => x.Description != null)
Price:             .GreaterThanOrEqualTo(0)
Currency:          .NotEmpty().Length(3)
MaxParticipants:   .GreaterThanOrEqualTo(1).When(x => x.MaxParticipants.HasValue)
ValidFrom:         .GreaterThan(DateTime.UtcNow).When(x => x.ValidFrom.HasValue)
ValidTo:           .GreaterThan(x => x.ValidFrom).When(x => x.ValidTo.HasValue)
IncludedTourIds:   .NotEmpty() — must have ≥2 unique Guids; no Guid.Empty
Inclusions:        .NotEmpty() — each: Description .NotEmpty().MaximumLength(500), SortOrder .GreaterThanOrEqualTo(0)

-- AddPackageInclusion --
PackageId:         .NotEqual(Guid.Empty)
Description:       .NotEmpty().MaximumLength(500)
```

---

## Domain Events & Integration Events — Master Index

Each task's full event / handler specification lives inline with that task (see "Domain & Integration Event Handler Logic" sections). This table is a **cross-reference only** — consult it to see what any given module should expect from ContentTours.

### Domain Events (in-module, dispatched by UoW, never cross module boundaries)

| Event | Task | Raised by | Handler writes to outbox | Integration event produced |
|---|---|---|---|---|
| `TourCreatedDomainEvent` | 1 | `Tour.Create()` | Yes | `TourCreatedIntegrationEvent` |
| `TourUpdatedDomainEvent` (includes `ChildrenInfoChanged`) | 1 + 4 | `Tour.Update()`, `Tour.UpdateChildrenInfo()` | Yes | `TourUpdatedIntegrationEvent` |
| `TourSubmittedDomainEvent` | 1 | `Tour.Submit()` | Yes | `TourSubmittedIntegrationEvent` |
| `TourApprovedDomainEvent` | 1 | `Tour.Approve()` | Yes | `TourApprovedIntegrationEvent` |
| `TourRejectedDomainEvent` | 1 | `Tour.Reject()` | Yes | `TourRejectedIntegrationEvent` |
| `TourSuspendedDomainEvent` | 1 | `Tour.Suspend()` | Yes | `TourSuspendedIntegrationEvent` |
| `TourReinstatedDomainEvent` | 1 | `Tour.Reinstate()` | Yes | `TourReinstatedIntegrationEvent` |
| `TourFeaturedChangedDomainEvent` | 3 | `Tour.SetFeatured()` | Yes | `TourFeaturedChangedIntegrationEvent` |
| `TourPackageCreatedDomainEvent` | 5 | `TourPackage.Create()` | Yes | `TourPackageCreatedIntegrationEvent` |
| `TourPackageUpdatedDomainEvent` | 5 | `TourPackage.Update()` | Yes | `TourPackageUpdatedIntegrationEvent` |
| _(no event)_ | 1 | `DeleteTour` handler | direct write | `TourDeletedIntegrationEvent` |
| _(no event)_ | 2 | `CreateTourPricingTier` / `UpdateTourPricingTier` / `DeleteTourPricingTier` handlers | direct write | `TourPricingTierChangedIntegrationEvent` |
| _(no event)_ | 2 | Schedule handlers | direct write (optional) | `TourScheduleChangedIntegrationEvent` (skip unless Booking requests) |
| _(no event)_ | 4 | `AssignTourGuide` / `UnassignTourGuide` handlers | direct write | `TourGuideAssigned/UnassignedIntegrationEvent` |
| _(no event)_ | 5 | `DeleteTourPackage` handler | direct write | `TourPackageDeletedIntegrationEvent` |

### Integration Events Published by ContentTours (complete list)

| Event | Registry Key | Consumers | Task |
|---|---|---|---|
| `TourCreatedIntegrationEvent` | `content-tours.tour.created.v1` | ContentSeo, Analytics | 1 |
| `TourUpdatedIntegrationEvent` | `content-tours.tour.updated.v1` | ContentSeo | 1 (+4 via `ChildrenInfoChanged`) |
| `TourDeletedIntegrationEvent` | `content-tours.tour.deleted.v1` | Booking, Social, ContentSeo | 1 |
| `TourSubmittedIntegrationEvent` | `content-tours.tour.submitted.v1` | Messaging (admin queue) | 1 |
| `TourApprovedIntegrationEvent` | `content-tours.tour.approved.v1` | Messaging, ContentSeo, Analytics | 1 |
| `TourRejectedIntegrationEvent` | `content-tours.tour.rejected.v1` | Messaging | 1 |
| `TourSuspendedIntegrationEvent` | `content-tours.tour.suspended.v1` | Messaging, Booking, ContentSeo | 1 |
| `TourReinstatedIntegrationEvent` | `content-tours.tour.reinstated.v1` | Messaging, ContentSeo, Booking | 1 |
| `TourFeaturedChangedIntegrationEvent` | `content-tours.tour.featured-changed.v1` | Analytics, ContentSeo | 3 |
| `TourPricingTierChangedIntegrationEvent` | `content-tours.pricing-tier.changed.v1` | Finance, Analytics | 2 |
| `TourScheduleChangedIntegrationEvent` | `content-tours.schedule.changed.v1` | Booking (optional) | 2 |
| `TourGuideAssignedIntegrationEvent` | `content-tours.tour-guide.assigned.v1` | Messaging, Analytics | 4 |
| `TourGuideUnassignedIntegrationEvent` | `content-tours.tour-guide.unassigned.v1` | Messaging, Booking (future) | 4 |
| `TourPackageCreatedIntegrationEvent` | `content-tours.package.created.v1` | Finance, Messaging, Analytics, ContentSeo | 5 |
| `TourPackageUpdatedIntegrationEvent` | `content-tours.package.updated.v1` | Finance, ContentSeo | 5 |
| `TourPackageDeletedIntegrationEvent` | `content-tours.package.deleted.v1` | Booking, ContentSeo, Finance | 5 |
| `TourPackageInclusionAddedIntegrationEvent` | `content-tours.package-inclusion.added.v1` | Analytics (optional) | 5 |

**Registration rule** (per `agent-context.md` §N / Outbox Hardening PR 2): every event above MUST be registered in `SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs` with its stable logical key. Unregistered events throw on publish attempt.

### Integration Events Consumed by ContentTours (inbound)

| Event | Source Module | Handler Location | Status |
|---|---|---|---|
| `LanguageActivatedIntegrationEvent` | ContentCore | `ContentTours.Infrastructure/EventHandlers/LanguageActivatedIntegrationEventHandler.cs` | ✅ Already shipped — do NOT modify |

### Handler Placement Policy (recap)

| Handler type | Location | Why |
|---|---|---|
| Domain event handler that ONLY updates in-module state + queues outbox | `ContentTours.Application/EventHandlers/` (preferred) | Keeps domain-layer dependencies minimal |
| Domain event handler that needs `ContentToursDbContext` to write outbox rows | `ContentTours.Infrastructure/EventHandlers/` (current codebase pattern) | Outbox `OutboxMessage` type lives in SharedKernel.Infrastructure |
| Integration event handler (inbound from another module) | `ContentTours.Infrastructure/EventHandlers/` | Consumes DbContext + InboxStore |

**Golden rules** (same as ContentPlaces):
1. Domain events ONLY raised on `IAggregateRoot` (Tour, TourPackage). Non-aggregates cannot raise — handlers write outbox directly.
2. NEVER call `SaveChangesAsync` inside a domain event handler — UoW dispatches events BEFORE SaveChanges; handler writes piggyback.
3. Handlers log at `Information` on success, `Warning` on infra failure, never `Error` for business outcomes.
4. Every event record is `sealed record … : DomainEventBase | IntegrationEventBase` (SharedKernel types).

---

## Dependency Graph & Critical Path

```
Pre-Work (enum + UoW)                                    [M]
   │
   ▼
Task 1 (Mahmoud · Tour Core)                             [M-T-W-R-F · Week 1]
   ├───────────────┬────────────────┬────────────────┐
   ▼               ▼                ▼                ▼
Task 2 (Mohammad) Task 4 (Ezz)   Task 5 (Fadwa)    [W2 wait]
Schedules+Pricing Waypoints+etc. Packages           
   │                │                │
   ▼                │                │
Task 3 (Mohammad) ──┤                │
Search+Featured     │                │
                    │                │
   └────────────────┴────────────────┴─► Integration freeze (R W3)
                                         │
                                         ▼
                                     PR cutoff (F W3 17:00)
                                         │
                                         ▼
                                   Merge + release tag
```

**Critical path**: Pre-work → Task 1 → Task 2 → Task 3 = 4 + 44 + 28 + 24 = **100 hrs on Mahmoud+Mohammad's shared timeline**. Any slip on Task 1 slips everyone.

Mahmoud has a **16-hour idle gap** after Task 1 (F 17:00 Wk1 → M 09:00 Wk2 + 8 Wk2 hrs). Tech Lead may pull in authorization-hygiene backlog items from `agent-context.md` §8 during this gap — Mahmoud's call.

---

## Risk Register

| ID | Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|---|
| R-1 | `TourStatus` enum rewrite breaks seeding or existing tests | Medium | Blocker | Seed data uses `Draft=0`, which is unchanged numerically. CI runs seeder on every build. | Mahmoud |
| R-2 | Cross-module `IPlaceExistsService` / `IAttachmentQueryService` don't exist yet | High | Medium | Mahmoud stubs an in-module `IPlaceExistsService` backed by a direct `DbContext` cross-module query for this sprint; document TODO for proper abstraction later. | Mahmoud |
| R-3 | Full-text search SQL performance is poor | Medium | Medium | Add composite index on `(Status, IsDeleted)` + persisted computed column `SearchDocument` (nvarchar(max)) concatenating name/description. Migration: `AddTourSearchDocument`. If still slow, fall back to pagination-only (skip relevance sort). | Mohammad |
| R-4 | Waypoint reorder lacks RowVersion — silent concurrency loss | Medium | Low | Accept the trade-off this sprint. Wrap in serializable transaction. Document in code comment. Add RowVersion in Sprint+1. | Ezz |
| R-5 | `TourPackage` aggregate marker causes EF migration conflict | Low | High | Do in a dedicated micro-PR at the top of Task 5 (WBS 5.2). Tech Lead reviews in <4 hrs. If rejected, fall back to handler-direct outbox writes. | Fadwa |
| R-6 | Translation orchestrator hits Azure Translator quota under sprint-level load | Low | Low | Already rate-limited. Add per-module backoff (existing code). | Tech Lead |
| R-7 | Beginner overrun on estimated hours (Ezz / Fadwa) | High | Medium | Daily standup + pair programming slot every Wed afternoon. Tech Lead reserves 8 hrs across the sprint for pairing. | Tech Lead |

---

## Definition of Done (per task)

Before opening a PR, every team member self-verifies this list:

- [ ] `dotnet build YallaJo.sln -c Debug --nologo` → **0 errors, 0 new warnings**
- [ ] `dotnet test YallaJo.sln --nologo` → all passing on your branch
- [ ] `lsp_diagnostics` clean on every changed file
- [ ] Every endpoint has **either** `.WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.X, AppAction.Y))` **or** `.AllowAnonymous()`
- [ ] Every endpoint has `.WithName()`, `.WithSummary()`, `.Produces<T>()`
- [ ] POST/PUT endpoints have `.ProducesValidationProblem()`
- [ ] GET/{id}, PUT, DELETE endpoints have `.ProducesProblem(404)`
- [ ] POST/PUT (uniqueness) endpoints have `.ProducesProblem(409)`
- [ ] SubmitTour endpoint has `.ProducesProblem(422)`
- [ ] Every handler injects `ILogger<THandler>`
- [ ] Every async method accepts and forwards `CancellationToken ct`
- [ ] `Guid.CreateVersion7()` used everywhere (not `Guid.NewGuid()` — grep your diff)
- [ ] `DateTime.UtcNow` used everywhere (not `DateTime.Now` — grep your diff)
- [ ] All DTOs are `record` types (not `class`)
- [ ] All queries implement `ICacheableQuery` with fine-grained tags
- [ ] All command handlers inject `HybridCache` + call `RemoveByTagAsync` after save
- [ ] Error codes follow `{Entity}.{Reason}` convention
- [ ] `ICurrentUser` injected **only** where ownership / IDOR / self-comparison is required
- [ ] Every new repository / service / catalog registered in `DependencyInjection.cs`
- [ ] Every new permission appears in `Security.Infrastructure` seed log at startup: `"Seeding N permissions from M modules: …, ContentTours"`
- [ ] `Agents/error-log.md` updated with every mistake you made (and its prevention rule)
- [ ] PR description cites: WBS items completed, endpoints shipped, test deltas, any scope cuts or deferrals

## Definition of Done (sprint-level, by **Fri 2026-05-22 · 17:00**)

- [ ] All 39 endpoints implemented, discoverable in Swagger
- [ ] 0 endpoint authorization violations (`endpoint-authorization-audit.md` audit script re-runs clean)
- [ ] 0 `ICurrentUser` violations on new code
- [ ] `dotnet build YallaJo.sln -c Release --nologo` → 0 errors on `main` after merge
- [ ] `dotnet test YallaJo.sln --nologo` → ≥ 190 tests (171 current + 19 from sprint — minimum 1 per subtask)
- [ ] Migration runs clean on a fresh DB from zero: `dotnet ef database update --project ContentTours.Infrastructure --context ContentToursDbContext`
- [ ] Seed data loads: a `Tour` with status `Draft` (or `Approved` after seed patch), full set of packages, schedules, waypoints, pricing tiers, translations
- [ ] `agent-context.md` §11.1 updated: `ContentTours` row → ✅ with summary
- [ ] `agent-context.md` §11.2 Work Log appended: one entry per contributor
- [ ] Release tagged `v0.contenttours.1` — Tech Lead
- [ ] Demo to stakeholders on **Mon 2026-05-25 · 11:00 AST** (Tech Lead schedules)

---

## Daily Standup Protocol

- **09:30 AST**, 15 minutes strict, video on, cameras on
- Format: each contributor answers
  1. What I **shipped** yesterday (commits or merged PRs)
  2. What I **ship today** (specific WBS items)
  3. What **blocks** me right now (dependency / question / design)
- Tech Lead logs blockers to `Agents/sprint-blockers.md` with owner + ETA to unblock
- Missed standup without prior written notice → 2 warnings, then pair-code with Tech Lead for half a day
- **Async alternative** allowed only with Tech Lead's written approval (Slack thread before 09:30)

## Emergency Escalation

| Severity | Example | Who to ping | SLA |
|---|---|---|---|
| 🟡 Warning | "I don't understand the requirement for X" | Tech Lead (Slack DM) | 2 hrs |
| 🟠 Risk | "My task will slip by 1 day" | Tech Lead + all standup | Next standup |
| 🔴 Blocker | "Build is broken on main" / "DB migration fails" | Tech Lead (phone) + #yallajo-ops | 30 min |
| 🚨 Crisis | "Data loss in dev", "prod outage" | Tech Lead + CTO (phone) | Immediate |

---

## Reference Links (read before coding)

- `Agents/agent-context.md` — Rules, architecture, checklists, gotchas (§0–§11)
- `Agents/error-log.md` — Every past mistake + prevention rule (READ ERR-001 through ERR-017)
- `Agents/ContentPlaces-team-tasks.md` — Previous sprint reference (closed)
- `Agents/ContentPlaces-fixes-required.md` — Bug patterns to avoid repeating
- `Agents/YallaJo.md` §Phase 1 ContentTours — Endpoint specs
- `Agents/YallaJo Business Rules & Edge Cases.pdf` §ContentTours — Business-rule bible
- `Agents/patterns/caching-patterns.md` — HybridCache tag patterns
- `Agents/patterns/error-handling-patterns.md` — Try/catch policy
- `Agents/templates/` — Copy-paste templates for Command / Handler / Validator / Entity / Endpoint
- `Agents/decisions/ADR-001..004.md` — Architectural decisions (monolith, CQRS, no-Hangfire, Result pattern)

---

## Closing Note

This sprint is **intentionally harder** than ContentPlaces. You are shipping a state machine, a recurrence engine, a faceted-search ranker, a batch-reorder endpoint, and an atomic-booking precursor — all in 15 business days. The rules exist because the previous sprint discovered them the hard way. Read the error log. Ship green. Ask early.

_Good luck, team._ 🚀

— Tech Lead, 2026-05-03
