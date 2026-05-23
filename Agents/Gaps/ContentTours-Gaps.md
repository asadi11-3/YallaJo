# ContentTours Module — Audit Report

> **Audited**: 2025-07-15  
> **Module status**: 🟡 In Progress (Tour Core Task-1 complete with 50 tests)  
> **Overall score**: 7.0 / 10

---

## Table of Contents

1. [Module Overview](#1-module-overview)
2. [Architecture Compliance](#2-architecture-compliance)
3. [Endpoint Security Audit](#3-endpoint-security-audit)
4. [Rule Compliance Matrix](#4-rule-compliance-matrix)
5. [Gap 1 — ICurrentUser Misuse (HIGH)](#5-gap-1--icurrentuser-misuse-high)
6. [Gap 2 — Validator Spec Mismatches (MEDIUM)](#6-gap-2--validator-spec-mismatches-medium)
7. [Gap 3 — Missing Business Logic (MEDIUM-HIGH)](#7-gap-3--missing-business-logic-medium-high)
8. [Gap 4 — Infrastructure Runtime Throws (LOW)](#8-gap-4--infrastructure-runtime-throws-low)
9. [What Passed — Full Checklist](#9-what-passed--full-checklist)
10. [Scorecard](#10-scorecard)
11. [Fix Priority & Recommendations](#11-fix-priority--recommendations)
12. [Appendix — Files Audited](#12-appendix--files-audited)

---

## 1. Module Overview

**Purpose**: Manages tour listings, pricing tiers, schedules, tour guides, packages, waypoints, children-friendly info, and tour search — the core catalog for YallaJo's tourism platform.

### Structure

| Project | Files | Purpose |
|---------|-------|---------|
| ContentTours.Domain | ~42 | 14 entities, 6 enums, 14 domain events, 8 repository interfaces |
| ContentTours.Application | ~164 | 44 handlers (29 command + 15 query), 28 validators, 7 interfaces, 7 cache key classes |
| ContentTours.Contracts | ~25 | 7 features, 27 permissions, 23 integration events, 2 cross-module contracts |
| ContentTours.Infrastructure | ~60 | 17 event handlers (14 domain + 3 integration), 16 EF configs, 8 repositories, 6 services, 7 migrations, 1 seeder |
| ContentTours.Presentation | ~10 | 9 endpoint groups (45 endpoints total) |
| ContentTours.Tests.Unit | ~37 | Unit tests (50 tests as per agent-context) |
| **Total** | **~338** | |

### Entities (14)

| Entity | Purpose |
|--------|---------|
| `Tour` | Main aggregate — lifecycle, pricing, location, children info, discount fields |
| `TourChildFacility` | Junction: child-friendly facility types for a tour |
| `TourGuide` | Separate aggregate — guide profile, languages, specializations |
| `TourGuideLanguage` | Junction: guide ↔ language |
| `TourGuideSpecialization` | Junction: guide ↔ specialization |
| `TourPackage` | Bundle aggregate — groups tours with inclusions + validity range |
| `TourPackageInclusion` | What's included in a package |
| `TourPackageTour` | Junction: package ↔ tour |
| `TourPricingTier` | Per-participant-type pricing (Adult, Child, Senior, etc.) |
| `TourPricingTierTranslation` | Localized pricing tier names |
| `TourSchedule` | Day-of-week schedule with time slots |
| `TourTourGuide` | Junction: tour ↔ guide assignment |
| `TourTranslation` | Multi-language tour content |
| `TourWaypoint` | Ordered stops along tour route |

### Enums (6)

| Enum | Values |
|------|--------|
| `TourStatus` | Draft=0, Pending=1, Approved=2, Rejected=3, Suspended=4, Archived=5 |
| `Difficulty` | Easy, Moderate, Hard, Expert |
| `ParticipantType` | Adult=1, Child=2, Senior=3, Student=4, Infant=5, Group=6, Family=7, Other=255 |
| `WaypointType` | Start, Stop, Meal, Photo, LandMark, RestStop, End |
| `ChildFacility` | 10 values (stroller, highchair, etc.) |
| `TourStatusExtensions` | Helper methods: IsEditable (Draft/Rejected), IsSubmittable, RequiresAdultTier |

### Repository Interfaces (8)

ITourRepository, ITourGuideRepository, ITourPackageRepository, ITourPricingTierRepository, ITourPricingTierTranslationRepository, ITourScheduleRepository, ITourTourGuideRepository, ITourWaypointRepository

### Integration Events (23)

Tour lifecycle: TourCreated, TourUpdated, TourDeleted, TourApproved, TourRejected, TourSubmitted, TourSuspended, TourReinstated, TourFeaturedChanged, TourPricingTierChanged, TourScheduleChanged  
Place interaction: PlaceTourCountUpdated  
Guide events: TourGuideRegistered, TourGuideUpdated, TourGuideLanguageAdded, TourGuideLanguageRemoved, TourGuideSpecializationAdded, TourGuideAssigned, TourGuideUnassigned  
Package events: TourPackageCreated, TourPackageUpdated, TourPackageDeleted  
Change discriminator: TourEntityChangeType

### Permission Catalog (27 permissions)

| Feature | Actions | Group |
|---------|---------|-------|
| Tour | Read, Create, Update, Delete, Submit, Approve, Reject, Suspend, Reinstate, ReadOwn, ReadAny, Feature | ContentManagement |
| Package | Read, Create, Update, Delete | ContentManagement |
| TourGuide | Update | ContentManagement |
| TourPricingTier | Create, Update, Delete | ContentManagement |
| TourSchedule | Create, Update, Delete | ContentManagement |
| TourWaypoint | Create, Update, Delete | ContentManagement |
| TourChildrenInfo | Update | ContentManagement |

### Cross-Module Contracts (Outgoing)

| Contract | Used By |
|----------|---------|
| `ITourExistenceService` | Other modules checking tour existence |
| `ITourOwnershipService` | Other modules verifying tour ownership |
| `TourExistenceStatus` | Status enum for existence checks |

### Cross-Module Dependencies (Incoming)

| Dependency | Used In |
|-----------|---------|
| `ContentPlaces.Contracts.Places.IPlaceExistenceService` | CreateTour, UpdateTour, SubmitTour handlers |
| `ContentCore.Contracts.Attachments.IAttachmentExistenceService` | SubmitTour handler |
| `Accounts.Contracts.IntegrationEvents` | ProviderSuspended/Reinstated handlers |
| `Security.Contracts.Authorization` | Multiple handlers + endpoints |

---

## 2. Architecture Compliance

### Dependency Graph

```
Presentation → Application → Domain ✅ (correct flow)
Infrastructure → Application → Domain ✅ (correct flow)
Contracts: standalone ✅ (no upward deps)
No circular dependencies detected ✅
```

### CQRS Pattern

| Metric | Count |
|--------|-------|
| Command handlers | 29 |
| Query handlers | 15 |
| Validators | 28 |
| Total handlers | 44 |

---

## 3. Endpoint Security Audit

### TourEndpoints.cs (11 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tours` | AllowAnonymous | ✅ |
| GET | `/tours/{id}` | AllowAnonymous | ✅ |
| GET | `/tours/{slug}` (by slug) | AllowAnonymous | ✅ |
| POST | `/tours` | MustHavePermission(Tour, Create) | ✅ |
| PUT | `/tours/{id}` | MustHavePermission(Tour, Update) | ✅ |
| DELETE | `/tours/{id}` | MustHavePermission(Tour, Delete) | ✅ |
| POST | `/tours/{id}/submit` | MustHavePermission(Tour, Submit) | ✅ |
| POST | `/tours/{id}/approve` | MustHavePermission(Tour, Approve) | ✅ |
| POST | `/tours/{id}/reject` | MustHavePermission(Tour, Reject) | ✅ |
| POST | `/tours/{id}/suspend` | MustHavePermission(Tour, Suspend) | ✅ |
| POST | `/tours/{id}/reinstate` | MustHavePermission(Tour, Reinstate) | ✅ |

### TourSearchEndpoints.cs (6 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tours/search` | AllowAnonymous | ✅ |
| GET | `/tours/search/suggest` | AllowAnonymous | ✅ |
| GET | `/tours/featured` | AllowAnonymous | ✅ |
| GET | `/tours/nearby` | AllowAnonymous | ✅ |
| GET | `/tours/provider/my-tours` | MustHavePermission(Tour, ReadOwn) | ✅ |
| PATCH | `/tours/{id}/feature` | MustHavePermission(Tour, Feature) | ✅ |

### TourPricingTierEndpoints.cs (4 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tours/{tourId}/pricing-tiers` | AllowAnonymous | ✅ |
| POST | `/tours/{tourId}/pricing-tiers` | MustHavePermission(TourPricingTier, Create) | ✅ |
| PUT | `/tours/{tourId}/pricing-tiers/{id}` | MustHavePermission(TourPricingTier, Update) | ✅ |
| DELETE | `/tours/{tourId}/pricing-tiers/{id}` | MustHavePermission(TourPricingTier, Delete) | ✅ |

### TourScheduleEndpoints.cs (4 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tours/{tourId}/schedules` | AllowAnonymous | ✅ |
| POST | `/tours/{tourId}/schedules` | MustHavePermission(TourSchedule, Create) | ✅ |
| PUT | `/tours/{tourId}/schedules/{id}` | MustHavePermission(TourSchedule, Update) | ✅ |
| DELETE | `/tours/{tourId}/schedules/{id}` | MustHavePermission(TourSchedule, Delete) | ✅ |

### TourGuideEndpoints.cs (3 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tours/{tourId}/guides` | AllowAnonymous | ✅ |
| POST | `/tours/{tourId}/guides` | MustHavePermission(TourGuide, Update) | ✅ |
| DELETE | `/tours/{tourId}/guides/{guideId}` | MustHavePermission(TourGuide, Update) | ✅ |

### TourGuideProfileEndpoints.cs (5 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tour-guides/{id}` | AllowAnonymous | ✅ |
| PUT | `/tour-guides/profile` | MustHavePermission + RequireAuthorization | ✅ |
| POST | `/tour-guides/languages` | MustHavePermission + RequireAuthorization | ✅ |
| DELETE | `/tour-guides/languages/{languageId}` | MustHavePermission + RequireAuthorization | ✅ |
| POST | `/tour-guides/specializations` | MustHavePermission + RequireAuthorization | ✅ |

### TourPackageEndpoints.cs (5 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/packages` | AllowAnonymous | ✅ |
| GET | `/packages/{id}` | AllowAnonymous | ✅ |
| POST | `/packages` | MustHavePermission(Package, Create) | ✅ |
| PUT | `/packages/{id}` | MustHavePermission(Package, Update) | ✅ |
| DELETE | `/packages/{id}` | MustHavePermission(Package, Delete) | ✅ |

### TourWaypointEndpoints.cs (4 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tours/{tourId}/waypoints` | AllowAnonymous | ✅ |
| POST | `/tours/{tourId}/waypoints` | MustHavePermission(TourWaypoint, Create) | ✅ |
| PUT | `/tours/{tourId}/waypoints/reorder` | MustHavePermission(TourWaypoint, Update) | ✅ |
| DELETE | `/tours/{tourId}/waypoints/{id}` | MustHavePermission(TourWaypoint, Delete) | ✅ |

### ChildrenInfoEndpoints.cs (2 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/tours/{tourId}/children-info` | AllowAnonymous | ✅ |
| PUT | `/tours/{tourId}/children-info` | MustHavePermission(TourChildrenInfo, Update) | ✅ |

**Result: 45/45 endpoints have correct authorization** ✅

---

## 4. Rule Compliance Matrix

| # | Rule | Status | Evidence |
|---|------|--------|----------|
| 1 | MustHavePermission on every endpoint | ✅ PASS | 45/45 endpoints verified |
| 2 | ICurrentUser only for ownership | ❌ FAIL | 22 handlers with auth gate/admin bypass (§5) |
| 3 | Result pattern (no throws in Application) | ✅ PASS | Zero `throw new` in 164 Application files |
| 4 | Per-module IPermissionCatalog | ✅ PASS | ContentToursPermissionCatalog with 27 permissions |
| 5 | No SaveChanges in domain event handlers | ✅ PASS | 14 domain handlers clean; 3 integration handlers use SaveChanges (acceptable) |
| 6 | DateTime.UtcNow only | ✅ PASS | Zero violations in Application + Domain |
| 7 | Guid.CreateVersion7 | ✅ PASS | Zero Guid.NewGuid() in Application + Domain |
| 8 | FluentValidation on commands | ✅ PASS | 28 validators present |
| 9 | HybridCache usage | ✅ PASS | 29 handlers use RemoveByTagAsync invalidation |
| 10 | Outbox pattern | ✅ PASS | OutboxWriter injected; domain handlers enqueue events |
| 11 | Spec business rules alignment | ⚠️ PARTIAL | Multiple validator/logic gaps (§6, §7) |
| 12 | Infrastructure throws review | ⚠️ LOW | 1 runtime throw in LanguageActivatedHandler (§8) |

---

## 5. Gap 1 — ICurrentUser Misuse (HIGH)

### Severity: HIGH  
### Impact: Violates Non-Negotiable Rule #2  
### Rule Violated: "ICurrentUser only for ownership — never for authorization decisions"

### Affected Handlers (22)

**Auth gate only (3)** — check `IsAuthenticated` / `UserId is null`:
- `CreateTourCommandHandler`
- `ApproveTourCommandHandler`
- `RejectTourCommandHandler`

**Admin-tier bypass only (11)** — check `AppRoles.HighestPrivilegeLevel >= Admin`:
- `AddTourWaypointCommandHandler`
- `RemoveTourWaypointCommandHandler`
- `ReorderTourWaypointsCommandHandler`
- `CreateTourScheduleCommandHandler`
- `UpdateTourScheduleCommandHandler`
- `DeleteTourScheduleCommandHandler`
- `CreateTourPricingTierCommandHandler`
- `UpdateTourPricingTierCommandHandler`
- `DeleteTourPricingTierCommandHandler`
- `AssignTourGuideCommandHandler`
- `UnassignTourGuideCommandHandler`

**Auth gate + admin-tier bypass (8)**:
- `CreateTourPackageCommandHandler`
- `UpdateTourPackageCommandHandler`
- `DeleteTourPackageCommandHandler`
- `AddInclusionCommandHandler`
- `UpdateTourCommandHandler`
- `DeleteTourCommandHandler`
- `SubmitTourCommandHandler`
- `UpdateTourChildrenInfoCommandHandler`

### Acceptable Usage (Non-violations)

- `ToggleTourFeaturedCommandHandler` — uses UserId for audit stamp only, not access control ✅
- `SuspendTourCommandHandler` — uses UserId for audit stamp only ✅
- 4 TourGuideProfile handlers — use `request.CallerUserId` for ownership, not ICurrentUser directly ✅

### Violation Pattern (repeated in all 22 handlers)

```csharp
// Auth gate — should be handled by endpoint middleware
if (currentUser.UserId is null)
{
    return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);
}

// Admin-tier bypass — should be endpoint-level authorization policy
var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;
if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId.Value)
{
    return Result.Failure(new Error("Tour.NotOwner", "..."), Outcome.Forbidden);
}
```

### Why This Is Wrong

1. **Auth gate** (`IsAuthenticated` check) duplicates middleware — RequireAuthorization already handles this
2. **Admin-tier bypass** makes authorization decisions in handler code — should be a custom authorization policy or endpoint attribute
3. **Ownership check** (`CreatedByUserId != currentUser.UserId.Value`) is the ONLY acceptable use of ICurrentUser

### Required Fix

Extract auth gate and admin bypass into a reusable authorization policy. Handlers should only use ICurrentUser for:
- `currentUser.UserId` to pass as ownership context (e.g., `createdByUserId`)
- Ownership comparisons (`entity.OwnerId != currentUser.UserId`)

---

## 6. Gap 2 — Validator Spec Mismatches (MEDIUM)

### Severity: MEDIUM  
### Impact: Validation rules don't match spec requirements  
### Rule Violated: Business rules from spec not enforced

### 6.1 MaxGroupSize: Validator allows 1-500, Spec says 1-100

**File**: `CreateTourCommandValidator.cs` (line 26-28), `UpdateTourCommandValidator.cs`

```csharp
// Current:
RuleFor(x => x.MaxGroupSize)
    .GreaterThan(0)
    .LessThanOrEqualTo(500);

// Spec requires: 1-100
```

**Domain entity (Tour.cs)** also validates 1-500 in `ValidateMaxGroupSize()`.

### 6.2 DurationMinutes: Validator allows >0 to 43200, Spec says 15-2880

**File**: `CreateTourCommandValidator.cs` (line 22-24)

```csharp
// Current:
RuleFor(x => x.DurationMinutes)
    .GreaterThan(0)
    .LessThanOrEqualTo(43200);  // 30 days!

// Spec requires: 15-2880 (15 min to 48 hours)
```

### 6.3 BasePrice: Validator allows ≥0, Spec says >0

**File**: `CreateTourCommandValidator.cs` (line 30-31)

```csharp
// Current:
RuleFor(x => x.BasePrice)
    .GreaterThanOrEqualTo(0m);  // allows free tours

// Spec requires: BasePrice > 0
```

**Note**: Allowing `BasePrice = 0` may be intentional for free tours. If so, this should be documented as a deliberate deviation from spec.

### Required Fix

Update validators AND domain entity validation methods to match spec ranges, or document deliberate deviations.

---

## 7. Gap 3 — Missing Business Logic (MEDIUM-HIGH)

### 7.1 No Unique Tour Name Per Provider

**Spec says**: "unique name per provider"  
**Actual**: No uniqueness check on tour name in CreateTour or UpdateTour handlers. Only slug uniqueness is enforced via `IsSlugReservedAsync()`.

**Required**: Add `IsNameTakenByProviderAsync(name, providerId, excludeTourId?)` to ITourRepository and call it in CreateTour and UpdateTour handlers.

### 7.2 No Max 50 Active Tours Per Provider

**Spec says**: "max 50 active tours" per provider  
**Actual**: No count check in CreateTourCommandHandler. A provider can create unlimited tours.

**Required**: Add `CountActiveByProviderAsync(providerId)` to ITourRepository, check `< 50` before creating.

### 7.3 No Critical-Field Re-Review (Different from Spec)

**Spec says**: Updating critical fields (name, price, location, duration) on an Approved tour should reset status to Pending for re-review.  
**Actual**: `Tour.Update()` calls `EnsureMutable()` which BLOCKS all edits unless status is Draft or Rejected. An Approved tour cannot be edited at all.

```csharp
private void EnsureMutable()
{
    if (!Status.IsEditable())  // IsEditable = Draft or Rejected only
        throw new InvalidOperationException("...");
}
```

**Analysis**: The current implementation is **more restrictive** than the spec. The spec allows editing Approved tours (with critical fields triggering re-review), but the code forbids ALL edits to Approved tours. This may be intentional for v1 — the provider must create a new tour instead.

**Decision needed**: Either implement spec's critical-field re-review pattern OR document this as an intentional v1 simplification.

### 7.4 No Archive Endpoint

**Spec**: `TourStatus.Archived` exists in the enum (value 5).  
**Actual**: No `Archive()` method on Tour entity, no ArchiveTour handler, no endpoint.

**Impact**: Dead enum value. Tours can never transition to Archived.

### 7.5 Search Uses LIKE Not Full-Text

**Spec says**: "full-text + faceted + autocomplete"  
**Actual**:
- **Full-text**: `SearchToursQueryHandler` uses `string.Contains()` → SQL `LIKE '%term%'`, not SQL Server full-text indexing. No relevance scoring or fuzzy matching.
- **Faceted filters**: ✅ Implemented (price, difficulty, duration, child-friendly, accessible, instant booking, discount, rating)
- **Autocomplete**: ✅ Implemented — `GET /tours/search/suggest` endpoint exists, matches Tour.Name + TourTranslation.Name

```csharp
// Current search implementation (line 42-48):
foreach (var token in tokens)
{
    var t = token;
    filtered = filtered.Where(x =>
        x.Name.Contains(t) ||
        (x.Description != null && x.Description.Contains(t)));
}
```

**Remaining gap**: Only the full-text indexing is missing. Faceted search and autocomplete are implemented.

### 7.6 No ProviderApplication Guard on Tour Creation

**Spec says**: Only approved service providers can create tours.  
**Actual**: `CreateTourCommandHandler` does NOT check if the user has an approved `ProviderApplication` in the Accounts module. Any authenticated user with `Tour.Create` permission can create tours.

**Note**: This is the same cross-module gap identified in ContentPlaces (Gap 7.8). The guard should be implemented consistently across both modules.

### 7.7 Package Spec Compliance — Partial

**Spec says**: Admin-only creation, 2-10 items, 1 tour anchor, atomic booking, most-restrictive refund policy.  
**Actual**:
- Admin-only: ❌ Endpoint uses `MustHavePermission(Package, Create)` — not admin-specific
- 2-10 items: TourPackage entity requires `≥2 distinct tours`, but no upper bound of 10 checked
- Tour anchor: Not enforced (spec says 1 tour must be the "anchor")
- Atomic booking: Not implemented yet (Booking module)
- Most-restrictive refund: Not implemented yet (Booking module)

---

## 8. Gap 4 — Infrastructure Runtime Throws (LOW)

### Severity: LOW  
### Impact: Runtime exceptions in infrastructure instead of Result pattern  
### Affected: 1 file

### 8.1 LanguageActivatedIntegrationEventHandler.cs (line 191)

```csharp
throw new InvalidOperationException("...");
```

**Context**: This is an integration event handler that handles language activation from ContentCore. The throw occurs in a runtime code path, not a startup/config guard.

**Note**: `DependencyInjection.cs:28` (startup guard) and `ContentToursDbInitializer.cs:351,367` (seeding) are ACCEPTABLE throws.

### Previously Reported — Now Corrected

~~`ProviderSuspendedSuspendToursHandler.cs` and `ProviderReinstatedReinstateToursHandler.cs`~~ — These are **integration event handlers** (not domain event handlers). Their `SaveChangesAsync` calls are **correct** as integration handlers operate in their own scope.

---

## 9. What Passed — Full Checklist

### Endpoint Authorization (44/44) ✅

Every endpoint has proper `MustHavePermission`, `AllowAnonymous`, or `RequireAuthorization` attributes. No unprotected endpoints found.

### Result Pattern ✅

Zero `throw new` in the entire Application layer (164 files searched). All handlers consistently use `Result.Success()` / `Result.Failure()` / `Result.Fail()`.

### SaveChanges in Event Handlers ✅

All 14 domain event handlers have explicit `// no SaveChanges — UoW commits atomically` comments. The 3 integration event handlers (ProviderSuspended, ProviderReinstated, LanguageActivated) correctly use SaveChanges in their own scope.

### Permission Catalog ✅

Complete `ContentToursPermissionCatalog` with 27 permissions covering all 7 features. All endpoint groups map correctly to their permissions.

### DateTime Convention ✅

Zero `DateTime.Now` or `DateTime.Today` in Application or Domain. All timestamps use `DateTime.UtcNow`.

### GUID Convention ✅

Zero `Guid.NewGuid()` in Application or Domain. Uses `Guid.CreateVersion7` or framework-provided IDs.

### HybridCache ✅

29 handlers inject `HybridCache` for post-write cache invalidation via `RemoveByTagAsync`. Consistent use of `ContentToursCacheKeys` tag constants. No read-through (`GetOrCreate`) usage — cache reads are in query handlers.

### Outbox Pattern ✅

`IContentToursOutboxWriter` is injected by domain event handlers. All 14 domain handlers enqueue integration events to the outbox.

### FluentValidation ✅

28 validators cover all command inputs. Validators are auto-registered via DI.

### Concurrency Control ✅

`RowVersionUtil.Equal()` checks in UpdateTour, SubmitTour, and other mutation handlers. `DbUpdateConcurrencyException` is caught and converted to Result.Failure with Conflict outcome.

### Cross-Module Contracts ✅

Clean contract-based integration:
- Outgoing: `ITourExistenceService`, `ITourOwnershipService`
- Incoming: `IPlaceExistenceService`, `IAttachmentExistenceService`
- NoOp stubs for unrealized dependencies (`NoOpProfileLookupService`, `NoOpScheduleBookingCountService`, `NoOpTourCapacityService`, `NoOpUserRoleChecker`)

### Pre-Submit Validation ✅ (Excellent)

`SubmitTourCommandHandler` performs 7 validation checks before allowing submission:
1. At least 1 image attachment (cross-module via `IAttachmentExistenceService`)
2. At least 1 active pricing tier
3. At least 1 active schedule
4. Description ≥100 characters
5. MeetingPoint coordinates present
6. Linked Place still exists (cross-module via `IPlaceExistenceService`)
7. At least 1 active Adult pricing tier

### Slug Uniqueness ✅

`IsSlugReservedAsync()` called in both CreateTour and UpdateTour handlers. Update uses `excludeTourId` to allow keeping the same slug.

### Tour State Machine ✅ (Core transitions)

- `Create()` → Draft
- `Submit()` → Draft → Pending (with pre-validation)
- `Approve(reviewerId)` → Pending → Approved
- `Reject(reviewerId, reason)` → Pending → Rejected
- `Suspend(adminId, reason)` → Approved → Suspended
- `Reinstate(adminId)` → Suspended → Approved
- `Update()` → Rejected → Draft (auto-reset, clears rejection data)
- `EnsureMutable()` → Only allows edits in Draft/Rejected

### Recurrence Support ✅

`TourSchedulePattern` enum and `TourScheduleRecurrenceExpander` in Application layer handle schedule recurrence patterns. `TourScheduleOverlapChecker` validates no schedule conflicts.

---

## 10. Scorecard

| Area | Score | Notes |
|------|-------|-------|
| Endpoint Auth | 10/10 | 45/45 correct |
| ICurrentUser | 3/10 | 22 of 26 handlers violate rule #2 |
| Result Pattern | 10/10 | Zero throws in Application |
| SaveChanges Rule | 10/10 | Domain handlers clean; integration handlers correct |
| Conventions | 10/10 | DateTime.UtcNow, Guid.CreateVersion7, HybridCache all compliant |
| Permission Catalog | 10/10 | Complete, all endpoints mapped |
| Validators | 7/10 | Present but spec mismatches on ranges |
| Business Logic | 5/10 | Missing name uniqueness, tour cap, archive, provider guard, search quality |
| Cross-Module | 8/10 | Good contracts; missing ProviderApplication guard |
| Test Coverage | 8/10 | 50 tests, 37 test files |
| **Overall** | **7.0/10** | Strong technical foundation; validator gaps + missing business rules |

---

## 11. Fix Priority & Recommendations

### Priority 1 — ICurrentUser Handler Cleanup (HIGH, ~6h)

Extract auth gate and admin-tier bypass into a reusable authorization middleware or policy. 22 handlers need cleanup. Same pattern as ContentCore and ContentPlaces — should be done as a cross-cutting fix.

### Priority 2 — Validator Spec Alignment (MEDIUM, ~1h)

Update CreateTour/UpdateTour validators AND domain entity validation:
- `MaxGroupSize`: 1-500 → 1-100
- `DurationMinutes`: >0 to 43200 → 15-2880
- `BasePrice`: ≥0 → >0 (or document free tours as deliberate deviation)

### Priority 3 — Unique Tour Name Per Provider (MEDIUM, ~2h)

Add `IsNameTakenByProviderAsync()` to `ITourRepository` and implementation. Call in CreateTour and UpdateTour handlers.

### Priority 4 — Max 50 Active Tours Per Provider (MEDIUM, ~1h)

Add `CountActiveByProviderAsync()` check to CreateTourCommandHandler.

### Priority 5 — ProviderApplication Guard (HIGH, ~2h)

Add cross-module check in CreateTourCommandHandler: verify calling user has an Approved ProviderApplication before allowing tour creation. Same fix needed in ContentPlaces CreateBusiness.

### Priority 6 — Critical-Field Re-Review Decision (MEDIUM, ~3h)

Decide: allow editing Approved tours with critical-field re-review (spec) or keep current strict approach (no edits to Approved tours). If implementing spec, add selective field comparison and status reset logic.

### Priority 7 — Archive Endpoint (LOW, ~2h)

Add `Archive()` method to Tour entity, `ArchiveTourCommandHandler`, and endpoint. Consider: which statuses can transition to Archived?

### Priority 8 — Package Spec Compliance (LOW, ~2h)

- Add upper bound of 10 tours per package
- Implement admin-only restriction (or document current permission-based approach)
- Add "anchor tour" concept

### Priority 9 — Search Quality (LOW, ~3h)

- Migrate from `LIKE` to SQL Server Full-Text Index for relevance scoring
- Autocomplete endpoint already exists (`/search/suggest`) ✅
- Consider fuzzy matching for typo tolerance

### Priority 10 — LanguageActivatedHandler Throw (LOW, ~30min)

Replace `throw new InvalidOperationException()` with logging + graceful handling.

---

## 12. Appendix — Files Audited

### Fully Read (content examined line-by-line)

- `ContentTours.Domain/Entities/Tour.cs` (625 lines)
- `ContentTours.Application/Commands/Tour/CreateTour/CreateTourCommandHandler.cs` (139 lines)
- `ContentTours.Application/Commands/Tour/CreateTour/CreateTourCommandValidator.cs` (89 lines)
- `ContentTours.Application/Commands/Tour/UpdateTour/UpdateTourCommandHandler.cs` (191 lines)
- `ContentTours.Application/Commands/Tour/SubmitTour/SubmitTourCommandHandler.cs` (219 lines)
- `ContentTours.Application/Queries/Tour/SearchTours/SearchToursQueryHandler.cs` (167 lines)
- All 9 endpoint files (fully read for auth audit)

### Searched (grep/pattern matching)

- All 164 Application files: `throw new`, `DateTime.Now`, `DateTime.Today`, `Guid.NewGuid()`, `ICurrentUser`, `MaxGroupSize`, `InclusiveBetween`
- All 42 Domain files: `DateTime.Now`, `Guid.NewGuid()`, `EnsureMutable`, `Update(`, status transitions
- All 60 Infrastructure files: `throw new`, `SaveChangesAsync`
- All event handler files (17): SaveChanges audit

### Background Agent Scans

- Full module inventory (bg_6474021b): All 338 files cataloged
- Security + throws audit (bg_bc7fc4db): ICurrentUser usage in 25 handler files, throw audit
- Domain entity deep read (bg_a00b1cc4): All 14 entities, enums, value objects examined
- Spec gap analysis (bg_0c4ddd6b): Validators, business rules, missing features
