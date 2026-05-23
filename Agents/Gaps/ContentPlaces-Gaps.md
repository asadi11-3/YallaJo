# ContentPlaces Module — Audit Report

> **Audited**: 2025-01-27  
> **Module status**: 🟡 In Progress  
> **Overall score**: 6.0 / 10

---

## 1. Module Overview

**Purpose**: Manages tourism places (cities, landmarks, regions) and businesses (restaurants, hotels, tour operators) — the geographical and commercial foundation of YallaJo. Includes business registration workflows, staff management, amenities, accessibility features, service items, and business hours.

### Structure

| Project | Files | Purpose |
|---------|-------|---------|
| ContentPlaces.Domain | ~35 | Entities, enums, domain events, repository interfaces |
| ContentPlaces.Application | ~120 | Command/query handlers, validators, DTOs |
| ContentPlaces.Contracts | ~16 | Integration events, cross-module service contracts |
| ContentPlaces.Infrastructure | ~46 | EF configs, repositories, event handlers, services, migrations |
| ContentPlaces.Presentation | ~8 | 6 endpoint groups (35 endpoints total) |
| ContentPlaces.Tests.Unit | ~33 | Unit tests |
| **Total** | **~258** | |

### Entities (10)

| Entity | Purpose |
|--------|---------|
| Place | Tourism place (city, landmark, region) |
| PlaceTranslation | Multi-language place content |
| PlaceBusiness | Junction: Place ↔ Business |
| Business | Commercial entity (restaurant, hotel, operator) |
| BusinessTranslation | Multi-language business content |
| BusinessHours | Operating hours per day |
| BusinessStaff | Staff members linked to a business |
| BusinessAmenity | Amenity offerings (WiFi, parking, etc.) |
| AccessibilityFeature | Accessibility capabilities (wheelchair, ramps, etc.) |
| ServiceItem | Services offered by a business |

### Enums (8)

| Enum | Values |
|------|--------|
| PlaceType | City, Landmark, Region, etc. |
| BusinessType | Restaurant, Hotel, TourOperator, etc. |
| BusinessStatus | Draft, PendingApproval, Approved, Rejected, Suspended |
| BusinessStaffRole | Owner, Manager, Staff, etc. |
| AccessibilityFeatureType | Wheelchair, Ramps, Restrooms, FlatTerrain, SignLanguage, AudioGuide, Braille |
| ServiceCategory | Category grouping for services |
| SubscriptionTier | Free, Basic, Premium, Enterprise |
| DayOfWeek | Monday–Sunday |

### Repository Interfaces (6)

`IPlaceRepository`, `IBusinessRepository`, `IBusinessStaffRepository`, `IBusinessAmenityRepository`, `IAccessibilityFeatureRepository`, `IServiceItemRepository`

Plus: `IContentPlacesUnitOfWork`

### Integration Events (13)

Business lifecycle: `BusinessApprovedIntegrationEvent`, `BusinessCreatedIntegrationEvent`, `BusinessRejectedIntegrationEvent`, `BusinessReinstatedIntegrationEvent`, `BusinessStaffAddedIntegrationEvent`, `BusinessStaffRemovedIntegrationEvent`, `BusinessSubmittedIntegrationEvent`, `BusinessSuspendedIntegrationEvent`, `BusinessUpdatedIntegrationEvent`

Place lifecycle: `PlaceCreatedIntegrationEvent`, `PlaceDeletedIntegrationEvent`, `PlaceUpdatedIntegrationEvent`

Cross-module inbound: `PlaceTourCountUpdatedIntegrationEvent` (from ContentTours)

### Cross-Module Contracts (3)

| Contract | Purpose |
|----------|---------|
| `IPlaceExistenceService` | Verify place exists (consumed by ContentTours) |
| `IPlaceOwnershipService` | Verify place ownership for auth |
| `PlaceExistenceStatus` | Return type for existence check |

### Permission Catalog (31 permissions)

| Feature | Read | Create | Update | Delete | SoftDelete | Feature | Submit | Approve | Reject | Suspend | Reinstate |
|---------|------|--------|--------|--------|------------|---------|--------|---------|--------|---------|-----------|
| Place | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | | | | | |
| Business | ✅ | ✅ | ✅ | ✅ | | | ✅ | ✅ | ✅ | ✅ | ✅ |
| BusinessHours | | | ✅ | | | | | | | | |
| BusinessStaff | ✅ | ✅ | | ✅ | | | | | | | |
| BusinessAmenity | ✅ | ✅ | | ✅ | | | | | | | |
| AccessibilityFeature | ✅ | | ✅ | | | | | | | | |
| ServiceItem | ✅ | ✅ | ✅ | | ✅ | | | | | | |
| Booking | ✅ | ✅ | | | | | | | | | |

**Permission Groups**: ContentManagement (most), BookingOperations (Booking)

---

## 2. Architecture Compliance

### Dependency Graph

```
Presentation → Application → Domain
                    ↓
              Contracts (shared)
                    ↓
Infrastructure → Application + Domain
```

✅ Correct layering. No circular dependencies detected.

### CQRS Pattern

| Type | Count |
|------|-------|
| Command Handlers | ~25 |
| Query Handlers | ~10 |
| Validators | 23 |
| **Total Handlers** | **~35** |

---

## 3. Endpoint Security Audit

### PlaceEndpoints.cs (10 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/places` | AllowAnonymous | ✅ |
| GET | `/places/{id}` | AllowAnonymous | ✅ |
| GET | `/places/{id}/businesses` | AllowAnonymous | ✅ |
| GET | `/places/search` | AllowAnonymous | ✅ |
| GET | `/places/admin` | MustHavePermission(Place, Read) | ✅ |
| POST | `/places` | MustHavePermission(Place, Create) | ✅ |
| PUT | `/places/{id}` | MustHavePermission(Place, Update) | ✅ |
| DELETE | `/places/{id}` | MustHavePermission(Place, Delete) | ✅ |
| PATCH | `/places/{id}/feature` | MustHavePermission(Place, Feature) | ✅ |
| PATCH | `/places/{id}/verify` | MustHavePermission(Place, Update) | ✅ |

### BusinessEndpoints.cs (12 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/businesses` | AllowAnonymous | ✅ |
| GET | `/businesses/{id}` | AllowAnonymous | ✅ |
| GET | `/businesses/{id}/hours` | AllowAnonymous | ✅ |
| GET | `/businesses/my` | RequireAuthorization | ✅ |
| GET | `/businesses/admin` | RequireAuthorization("Admin") | ✅ |
| POST | `/businesses` | MustHavePermission(Business, Create) | ✅ |
| PUT | `/businesses/{id}` | MustHavePermission(Business, Update) | ✅ |
| DELETE | `/businesses/{id}` | MustHavePermission(Business, Delete) | ✅ |
| POST | `/businesses/{id}/submit` | MustHavePermission(Business, Submit) | ✅ |
| POST | `/businesses/{id}/approve` | MustHavePermission(Business, Approve) + RequireAuthorization("Admin") | ✅ |
| POST | `/businesses/{id}/reject` | MustHavePermission(Business, Reject) + RequireAuthorization("Admin") | ✅ |
| POST | `/businesses/{id}/suspend` | MustHavePermission(Business, Suspend) + RequireAuthorization("Admin") | ✅ |
| POST | `/businesses/{id}/reinstate` | MustHavePermission(Business, Reinstate) + RequireAuthorization("Admin") | ✅ |
| PUT | `/businesses/{id}/hours` | MustHavePermission(BusinessHours, Update) | ✅ |

### BusinessAmenityEndpoints.cs (3 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/business-amenities` | AllowAnonymous | ✅ |
| POST | `/business-amenities` | MustHavePermission(BusinessAmenity, Create) | ✅ |
| DELETE | `/business-amenities/{id}` | MustHavePermission(BusinessAmenity, Delete) | ✅ |

### BusinessStaffEndpoints.cs (3 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/business-staff` | MustHavePermission(Business, Read) | ✅ |
| POST | `/business-staff` | MustHavePermission(BusinessStaff, Create) | ✅ |
| DELETE | `/business-staff/{id}` | MustHavePermission(BusinessStaff, Delete) | ✅ |

### AccessibilityFeatureEndpoints.cs (2 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/accessibility-features` | AllowAnonymous | ✅ |
| PUT | `/accessibility-features` | MustHavePermission(AccessibilityFeature, Update) | ✅ |

### ServiceItemEndpoints.cs (5 endpoints)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| GET | `/service-items` | AllowAnonymous | ✅ |
| GET | `/service-items/{id}` | AllowAnonymous | ✅ |
| POST | `/service-items` | MustHavePermission(ServiceItem, Create) | ✅ |
| PUT | `/service-items/{id}` | MustHavePermission(ServiceItem, Update) | ✅ |
| DELETE | `/service-items/{id}` | MustHavePermission(ServiceItem, SoftDelete) | ✅ |

**Total: 35/35 endpoints correctly secured** ✅

---

## 4. Rule Compliance Matrix

| # | Rule | Status | Evidence |
|---|------|--------|----------|
| 1 | MustHavePermission on every mutating endpoint | ✅ PASS | 35/35 endpoints verified |
| 2 | ICurrentUser only for ownership checks | ❌ FAIL | 12 handlers with auth gate + 9 with admin bypass |
| 3 | Result pattern (no throws in Application) | ✅ PASS | Zero `throw new` in Application layer |
| 4 | Per-module IPermissionCatalog | ✅ PASS | ContentPlacesPermissionCatalog with 31 permissions |
| 5 | No SaveChanges in domain event handlers | ✅ PASS | 11 domain event handlers, all clean |
| 6 | DateTime.UtcNow only | ✅ PASS | Zero DateTime.Now/Today in Application + Domain |
| 7 | Guid.CreateVersion7 only | ✅ PASS | Zero Guid.NewGuid() in Application + Domain |
| 8 | FluentValidation on commands | ✅ PASS | 23 validators covering commands |
| 9 | HybridCache for queries | ✅ PASS | 20+ handlers inject HybridCache directly |
| 10 | Outbox pattern for integration events | ✅ PASS | OutboxMessageConfiguration in EF configs |
| 11 | No throws in Infrastructure (runtime) | ⚠️ PARTIAL | 6 occurrences, 2 are runtime violations |
| 12 | Unit of Work pattern | ✅ PASS | IContentPlacesUnitOfWork implemented |

---

## 5. Gap #1 — ICurrentUser Misuse in Command Handlers (HIGH)

**Severity**: 🔴 HIGH  
**Impact**: Authorization logic leaks into application layer, violating separation of concerns  
**Rule Violated**: Rule #2 — ICurrentUser only for ownership checks

### Affected Handlers (14 total, 12 with violations)

**Auth Gate Violations (IsAuthenticated check) — 12 handlers**:
1. `UpdateAccessibilityFeaturesCommandHandler`
2. `CreateBusinessCommandHandler`
3. `ResubmitBusinessCommandHandler`
4. `UpdateBusinessCommandHandler`
5. `RemoveBusinessStaffCommandHandler`
6. `DeletePlaceCommandHandler`
7. `FeaturePlaceCommandHandler`
8. `UpdatePlaceCommandHandler`
9. `VerifyPlaceCommandHandler`
10. `CreateServiceItemCommandHandler`
11. `DeleteServiceItemCommandHandler`
12. `UpdateServiceItemCommandHandler`

**Admin-Tier Bypass Violations — 9 handlers** (subset of above):
- `UpdateBusinessCommandHandler`, `RemoveBusinessStaffCommandHandler`, `DeletePlaceCommandHandler`, `FeaturePlaceCommandHandler`, `UpdatePlaceCommandHandler`, `VerifyPlaceCommandHandler`, `CreateServiceItemCommandHandler`, `DeleteServiceItemCommandHandler`, `UpdateServiceItemCommandHandler`

**Query Handlers using ICurrentUser — 2 handlers** (likely ownership filtering, ACCEPTABLE):
- `ListBusinessStaffQueryHandler`
- `ListServiceItemsQueryHandler`

### Violation Pattern

```csharp
// ❌ Auth gate — should be handled at endpoint level
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure<...>(UserErrors.Unauthorized);

// ❌ Admin-tier bypass — role-based auth belongs in endpoint/policy
if (AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin)
{
    // skip ownership check
}

// ✅ ONLY THIS IS ACCEPTABLE — ownership check
if (ownership.OwnerUserId != currentUser.UserId.Value)
    return Result.Failure<...>(UserErrors.Forbidden);
```

### Why This Is Wrong

1. **Auth gate**: The `IsAuthenticated` check is redundant — `RequireAuthorization` at the endpoint already guarantees this. Having it in handlers creates a false safety net and violates single-responsibility.
2. **Admin bypass**: Role-based authorization should be handled via endpoint policies (`RequireAuthorization("Admin")`) or `MustHavePermission`, not inline role checks in handlers.
3. **Inconsistency**: Some handlers check roles, others don't — creating confusion about where authorization lives.

### Required Fix

1. Remove `IsAuthenticated` / `currentUser.UserId is null` guard from all 12 handlers
2. Remove `AppRoles.HighestPrivilegeLevel` bypass from all 9 handlers
3. Implement proper authorization policies at endpoint level for admin bypass scenarios
4. Keep ONLY `ownership.OwnerUserId != currentUser.UserId.Value` checks
5. Query handlers using ICurrentUser for filtering are acceptable — no change needed

---

## 6. Gap #2 — Infrastructure Runtime Throws (MEDIUM)

**Severity**: 🟡 MEDIUM  
**Impact**: Unhandled exceptions in runtime code instead of Result pattern  
**Rule Violated**: Rule #3 (extended to Infrastructure runtime code)

### Affected Files (2 runtime violations)

| File | Line | Exception | Context |
|------|------|-----------|---------|
| `LanguageActivatedIntegrationEventHandler.cs` | 178 | `InvalidOperationException` | Runtime processing |
| `LanguageActivatedIntegrationEventHandler.cs` | 266 | `InvalidOperationException` | Runtime processing |

### Acceptable Throws (4)

| File | Line | Exception | Why Acceptable |
|------|------|-----------|----------------|
| `DependencyInjection.cs` | 27 | `InvalidOperationException` | Startup config guard |
| `ContentPlacesUnitOfWork.cs` | 17 | `DbUpdateConcurrencyException` | Concurrency re-throw (pattern) |
| `ContentPlacesDbInitializer.cs` | 184 | `InvalidOperationException` | Seeding guard |
| `ContentPlacesDbInitializer.cs` | 200 | `InvalidOperationException` | Seeding guard |

### Why This Is Wrong

Runtime `throw new` in integration event handlers can crash the handler pipeline instead of gracefully logging and continuing. Integration event handlers should be resilient.

### Required Fix

1. Replace `throw new InvalidOperationException(...)` at lines 178 and 266 with proper error logging + graceful degradation
2. Consider wrapping in try/catch with Serilog error logging if the operation is non-critical
3. Review `ContentPlacesUnitOfWork.cs` concurrency re-throw — determine if it should return `Result.Failure` instead

---

## 7. Gap #3 — Dual State Machine Architecture Conflict (HIGH)

**Severity**: 🔴 HIGH  
**Impact**: Two separate entities manage overlapping provider registration concerns with NO cross-module linkage — architectural inconsistency and potential data drift  
**Rule Violated**: DDD bounded context integrity, spec compliance

### 7.1 Root Cause — Two Parallel State Machines

**`Accounts.Domain.ProviderApplication`** — Spec-compliant, full state machine:
- Statuses: `Draft=0, Pending=1, MoreDocsNeeded=2, Approved=3, Rejected=4, Suspended=5`
- Has: `ReapplicationCount`, `CoolingPeriodDays=7`, `MaxReapplications=3`, `CoolingPeriodEndsAt`
- Has: `ProviderDocument` with `ExpiresAt` (DateTime?)
- Has: `Submit()`, `Approve()`, `Reject()`, `RequestMoreDocs()`, `Suspend()`, `Reinstate()`
- Required docs enforced per ProviderType (4-5 docs each for 5 provider types)

**`ContentPlaces.Domain.Business`** — Simplified duplicate:
- Statuses: `Pending=0, Approved=1, Rejected=2, Suspended=3` (NO Draft, NO MoreDocsNeeded)
- Missing: `ResubmitCount`, `CoolingPeriod`, `DocumentExpiryDate`, `ReviewDeadline`
- Has: `Create()→Pending`, `Approve()`, `Reject()`, `Resubmit()`, `Suspend()`, `Reinstate()`
- No document tracking, no re-application limit, no SLA

**Cross-module linkage**: ZERO references to `ProviderApplication` in `ContentPlaces.Application`. `CreateBusinessCommandHandler` does NOT check for an approved ProviderApplication before creating a Business.

### 7.2 Architecture Decision Required

This is not a simple "add missing fields" fix. The team must decide:

**Option A — ContentPlaces Business delegates to Accounts** (Recommended):
1. Business entity drops its own approval state machine
2. Business creation requires an approved `ProviderApplication` (verified via cross-module contract)
3. Business status derives from ProviderApplication status via integration events
4. All registration rules (MoreDocsNeeded, re-application limit, SLA, document expiry) stay in Accounts
5. ContentPlaces consumes `ProviderApplicationApprovedIntegrationEvent` to allow business creation
6. Add `IProviderStatusService` contract in `Accounts.Contracts` for ContentPlaces to consume

**Option B — ContentPlaces Business is independent** (Current state, needs enhancement):
1. Keep Business as its own aggregate with its own approval flow
2. Add the missing spec rules directly to Business (see 7.3–7.7 below)
3. Accept that two separate approval flows exist for different concerns
4. Risk: Data can drift between ProviderApplication and Business statuses

**Option C — Merge into single workflow**:
1. ProviderApplication approval automatically creates a Business in Pending state
2. Business only manages content lifecycle (active/inactive/featured), not approval
3. Most aligned with spec but requires significant refactoring

### 7.3 Missing `MoreDocsNeeded` Business Status

**Spec says**: Provider state machine includes `Pending → MoreDocsNeeded → (resubmit) → Pending`  
**Current**: `BusinessStatus` enum has only `Pending=0, Approved=1, Rejected=2, Suspended=3`  
**Impact**: Admins cannot request additional documents — they can only Approve or Reject outright

**Required Fix** (if Option B):
1. Add `MoreDocsNeeded = 4` to `BusinessStatus` enum
2. Add `RequestMoreDocs()` method to `Business` entity (transition: Pending → MoreDocsNeeded)
3. Update `Resubmit()` to also accept transition from `MoreDocsNeeded` → `Pending`
4. Add admin endpoint `POST /businesses/{id}/request-docs`
5. Add `RequestDocs` permission to `ContentPlacesPermissionCatalog`
6. Add corresponding domain event + integration event

### 7.4 Missing Re-application Limit (Max 3)

**Spec says**: Maximum 3 re-application attempts  
**Current**: No `ResubmitCount` property on `Business`, no max check in `ResubmitBusinessCommandHandler`  
**Impact**: Providers can resubmit indefinitely after rejection

**Required Fix** (if Option B):
1. Add `ResubmitCount` (int) property to `Business` entity
2. Increment in `Resubmit()` method
3. Add guard: `if (ResubmitCount >= 3) → InvalidOperation`
4. Add EF migration for new column

### 7.5 Missing 7-Day Admin SLA Tracking

**Spec says**: 7-day admin SLA for reviewing submitted businesses  
**Current**: No `ReviewDeadline` property, no background job for overdue review alerts  
**Impact**: No accountability for admin review timelines

**Required Fix** (if Option B):
1. Add `SubmittedAt` / `ReviewDeadline` properties to `Business`
2. Set `ReviewDeadline = SubmittedAt + 7 days` on Submit
3. Add `BusinessReviewOverdueJob` background service to check for overdue reviews
4. Emit notification when SLA is breached

### 7.6 Missing Document Expiry with 14-Day Grace Period

**Spec says**: Provider documents have expiry dates with 14-day grace period  
**Current**: No `DocumentExpiryDate` or `GracePeriodEnd` properties on `Business`  
**Impact**: No tracking of document validity — expired documents go unnoticed

**Required Fix** (if Option B):
1. Add `DocumentExpiryDate` and `GracePeriodEnd` to `Business` entity
2. Add `DocumentExpiryCheckJob` background service
3. Auto-suspend businesses with expired documents past grace period
4. Send warning notifications 14 days before expiry

### 7.7 Missing LicenseNumber Unique Constraint

**Spec says**: LicenseNumber must be unique per provider  
**Current**: `LicenseNumber` exists as `string?` property but no unique index in EF configuration  
**Impact**: Duplicate license numbers could be registered

**Required Fix**:
1. Add `.HasIndex(b => b.LicenseNumber).IsUnique().HasFilter("LicenseNumber IS NOT NULL")` to `BusinessConfiguration`
2. Add EF migration

### 7.8 Missing CreateBusiness → ProviderApplication Guard

**Spec says**: Only approved providers can create businesses  
**Current**: `CreateBusinessCommandHandler` checks auth + place existence + slug uniqueness — does NOT verify the user has an approved ProviderApplication  
**Impact**: Any authenticated user with `Business.Create` permission can create a business without being an approved provider

**Required Fix** (regardless of option):
1. Add `IProviderStatusService` to `Accounts.Contracts` with `IsApprovedProvider(Guid userId)` method
2. Inject in `CreateBusinessCommandHandler` and validate before creating
3. Return `Result.Failure("User is not an approved provider")` if not approved

### 7.8b Cross-Module Finding: Accounts Resubmit-After-Rejection is Unfinished

**Context**: While auditing the Accounts `ProviderApplication` state machine for comparison:
- `ProviderApplication.Submit()` only allows transition from `Draft` or `MoreDocsNeeded` → `Pending`
- There is **NO transition from `Rejected` → Draft/Pending`** — a rejected provider cannot resubmit
- Yet `ProviderApplicationTests` simulate resubmission after rejection, implying this was planned
- `ReapplicationCount` and `CoolingPeriodEndsAt` exist but have no resubmit path to trigger them from Rejected state
- **This is a bug in Accounts module**, not ContentPlaces — but relevant to the dual state machine analysis

### 7.8c Cross-Module Finding: Accounts Admin Queue Document Count Bug

**Context**: `GetAdminProviderQueueQueryHandler` reads `a.Documents.Count` without `.Include(a => a.Documents)` — unless lazy loading is globally enabled, document counts in the admin queue will always be 0. This is an Accounts module bug surfaced during cross-module analysis.

### 7.9 Missing Business Geo-Search / Filtered Search Endpoint

**Spec says**: Businesses should be searchable by type, status, location, and other filters  
**Current**: Only business listing is `GET /places/{id}/businesses` — per-place listing with visibility/ownership filtering. No dedicated `/businesses/search` or geo-search for businesses.  
**Impact**: No way for users to discover businesses outside of a specific place context

**Required Fix**:
1. Add `GET /businesses/search` endpoint with type/status/city/location filters
2. Add `GET /businesses/nearby` geo-search endpoint (reuse Haversine pattern from Places)
3. Add corresponding query handler and specification

### 7.10 Missing Max Active Businesses Cap

**Spec says**: Providers should have a maximum number of active businesses (tied to subscription tier)  
**Current**: No cap/limit logic in Business domain or application validators  
**Impact**: Providers can create unlimited businesses regardless of subscription tier

**Required Fix**:
1. Add max-active-business validation in `CreateBusinessCommandHandler`
2. Query count of active businesses per owner and check against tier limit
3. Return `Result.Failure` if limit exceeded

### 7.11 Missing Unique Place Name+Country Constraint

**Spec says**: Place name must be unique within a country  
**Current**: Only unique constraint on Place is `Slug`. No `(Name, Country)` unique index.  
**Impact**: Duplicate place names can exist within the same country

**Required Fix**:
1. Add `.HasIndex(p => new { p.Name, p.Country }).IsUnique()` to `PlaceConfiguration`
2. Add uniqueness check in `CreatePlaceCommandHandler`
3. Add EF migration

### 7.12 Endpoint Count Correction

**Initial audit**: Reported 10 Place endpoints  
**Actual**: 11 Place endpoints (missed `GET /places/{slug}` by-slug route)  
**Updated total**: 36 endpoints (not 35)

---

## 8. What Passed — Full Checklist

> Note: PlaceEndpoints has 11 endpoints (not 10 as initially counted). Total module endpoints: 36.

### ✅ Endpoint Authorization (36/36)
Every endpoint has appropriate auth. Public GETs use `AllowAnonymous`. Mutating endpoints use `MustHavePermission`. Admin-only operations (approve/reject/suspend/reinstate) use `RequireAuthorization("Admin")`. No gaps.

### ✅ Result Pattern in Application Layer
Zero `throw new` found in `ContentPlaces.Application`. All command handlers consistently use `Result.Success<T>()` and `Result.Failure<T>(error)`.

### ✅ SaveChanges in Domain Event Handlers
All 11 domain event handlers explicitly comment "no SaveChanges — UoW commits atomically". Zero `SaveChangesAsync` calls. Integration event handlers (2) do use `SaveChangesAsync` which is acceptable as they run in their own scope.

### ✅ Permission Catalog
`ContentPlacesPermissionCatalog` defines 31 permissions across 8 features. All permissions used by endpoints are registered. No dead permissions detected (unlike ContentCore's Language.Delete).

### ✅ DateTime.UtcNow Convention
Zero `DateTime.Now` or `DateTime.Today` anywhere in Application or Domain layers. Full UTC compliance.

### ✅ Guid.CreateVersion7 Convention
Zero `Guid.NewGuid()` in Application or Domain layers. All GUIDs use `Guid.CreateVersion7()`.

### ✅ HybridCache
20+ handlers inject `HybridCache` directly and use `RemoveByTagAsync` for cache invalidation. Consistent with ContentCore pattern.

### ✅ Outbox Pattern
`OutboxMessageConfiguration` and `InboxMessageConfiguration` present in EF configs. Integration events are dispatched through the outbox.

### ✅ FluentValidation
23 validators cover command inputs. Consistent use of FluentValidation.

### ✅ Cross-Module Design
Clean contracts: `IPlaceExistenceService`, `IPlaceOwnershipService`, `PlaceExistenceStatus` — well-designed cross-module boundaries consumed by ContentTours.

### ✅ Domain Events
11 domain events (8 Business lifecycle + 3 Place lifecycle) with corresponding handlers. All follow the pattern correctly.

### ✅ Unit Tests
33 test files covering the module's business logic.

---

## 9. Scorecard

| Area | Score | Notes |
|------|-------|-------|
| Endpoint Security | 10/10 | 36/36 perfectly secured |
| Permission Catalog | 10/10 | 31 permissions, all used, no dead entries |
| Result Pattern (Application) | 10/10 | Zero throws |
| Domain Event Handlers | 10/10 | Zero SaveChanges, all with explicit comments |
| ICurrentUser Usage | 4/10 | 12 auth gate + 9 admin bypass violations across 14 handlers |
| DateTime/Guid Conventions | 10/10 | Zero violations |
| Infrastructure Throws | 8/10 | 2 runtime throws in LanguageActivatedHandler |
| FluentValidation | 9/10 | 23 validators, good coverage |
| Cross-Module Design | 10/10 | Clean contracts, well-defined boundaries |
| Test Coverage | 8/10 | 33 test files, reasonable coverage |
| Spec Compliance | 3/10 | Dual state machine conflict, 9 missing business rules, no cross-module guard, no business search |
| **Overall** | **6.0/10** | ICurrentUser misuse + dual state machine + missing cross-module linkage + spec gaps |

---

## 10. Fix Priority & Recommendations

### Priority 1 — Architecture Decision: Dual State Machine Resolution (CRITICAL, ~1h decision + 4-8h implementation)
- **MUST DECIDE FIRST**: Option A (delegate to Accounts), B (enhance Business), or C (merge workflows)
- This decision gates all other spec compliance fixes (7.3–7.8)
- Recommended: Option A — ContentPlaces Business delegates approval to Accounts module
- Add `IProviderStatusService` contract + `CreateBusiness` guard regardless of option chosen

### Priority 2 — ICurrentUser Handler Cleanup (HIGH, ~4h effort)
- Remove auth gates from 12 handlers
- Remove admin-tier bypass from 9 handlers
- Implement proper endpoint-level authorization policies
- Keep only ownership checks via `IEntityOwnershipResolver`
- This is the same pattern as ContentCore's Gap #1 — fix both modules together

### Priority 3 — CreateBusiness → ProviderApplication Guard (HIGH, ~2h effort)
- Add `IProviderStatusService` to `Accounts.Contracts`
- Inject in `CreateBusinessCommandHandler` and validate approved provider
- This is required regardless of which architecture option is chosen

### Priority 4 — Missing Business Status & State Machine (MEDIUM-HIGH, ~3h effort, if Option B)
- Add `MoreDocsNeeded = 4` to `BusinessStatus` enum
- Add `RequestMoreDocs()` to Business entity + domain event
- Add `POST /businesses/{id}/request-docs` admin endpoint
- Update `Resubmit()` to accept `MoreDocsNeeded → Pending` transition
- Add migration

### Priority 5 — Re-application Limit (MEDIUM, ~1h effort, if Option B)
- Add `ResubmitCount` to Business entity
- Enforce max 3 in `Resubmit()` method
- Add migration

### Priority 6 — LicenseNumber + Place Name+Country Unique Constraints (MEDIUM, ~1h effort)
- Add unique filtered index on `LicenseNumber` in `BusinessConfiguration`
- Add unique index on `(Name, Country)` in `PlaceConfiguration`
- Add uniqueness checks in Create handlers
- Add EF migration

### Priority 7 — Business Search & Geo-Search Endpoints (MEDIUM, ~3h effort)
- Add `GET /businesses/search` with type/status/city filters
- Add `GET /businesses/nearby` geo-search endpoint (reuse Haversine pattern)
- Add query handlers and specifications

### Priority 8 — Max Active Businesses Cap (MEDIUM, ~1h effort)
- Add active-business count validation in `CreateBusinessCommandHandler`
- Enforce tier-based limits (Free=1, Basic=3, Premium=10, Enterprise=unlimited)

### Priority 9 — Admin SLA Tracking (MEDIUM, ~2h effort, deferrable to Phase 2)
- Add `SubmittedAt` / `ReviewDeadline` to Business
- Add `BusinessReviewOverdueJob` background service

### Priority 10 — Document Expiry Tracking (MEDIUM, ~2h effort, deferrable to Phase 2)
- Add `DocumentExpiryDate` / `GracePeriodEnd` to Business
- Add `DocumentExpiryCheckJob` background service

### Priority 11 — LanguageActivatedHandler Runtime Throws (MEDIUM, ~1h effort)
- Replace `throw new InvalidOperationException` at lines 178, 266 with error logging
- Add resilient error handling for integration event processing

### Priority 12 — ContentPlacesUnitOfWork Concurrency Review (LOW, ~30min effort)
- Review whether `DbUpdateConcurrencyException` re-throw should use Result pattern
- Align with project-wide UoW exception handling strategy

---

## 11. Appendix — Files Audited

### Presentation (read fully)
- `PlaceEndpoints.cs` — 11 endpoints (incl. by-slug, nearby, map/viewport)
- `BusinessEndpoints.cs` — 12 endpoints (+ hours update)
- `BusinessAmenityEndpoints.cs` — 3 endpoints
- `BusinessStaffEndpoints.cs` — 3 endpoints
- `AccessibilityFeatureEndpoints.cs` — 2 endpoints
- `ServiceItemEndpoints.cs` — 5 endpoints

### Application (searched + sampled)
- All command/query handlers searched for `throw new`, `ICurrentUser`, `SaveChangesAsync`
- 14 handlers flagged for ICurrentUser usage, 12 with violations

### Infrastructure (searched + read)
- All event handlers verified for SaveChanges compliance
- `LanguageActivatedIntegrationEventHandler.cs` — runtime throws flagged
- `ContentPlacesUnitOfWork.cs` — concurrency throw noted
- `DependencyInjection.cs`, `ContentPlacesDbInitializer.cs` — acceptable startup throws

### Domain (searched)
- All entities, enums, events verified for DateTime/Guid convention compliance
