# ContentPlaces-Workflow.md — Fix Plan

> **Source**: ContentPlaces-Audit-Report.md
> **Created**: 2025-07-07
> **Fixes**: 9
> **Estimated Effort**: 3-4 hours
> **Risk**: Low-Medium

---

## Design Decisions for Fixes

| # | Decision | Value | Rationale |
|---|----------|-------|-----------|
| 1 | ProviderSuspended handler pattern | Iterate + domain method | Consistent with Accounts module suspension pattern |
| 2 | ReviewCreated → rating update | Via Place/Business.UpdateRating() | Domain methods already exist |
| 3 | BusinessMoreDocsRequested handler | Domain event → outbox | Consistent with all other Business domain events |
| 4 | PlaceUpdatedIntegrationEvent payload | Add Slug + OldSlug | ContentSeo needs it for sitemap/301 redirects |
| 5 | AccessibilityEntityType constants | Static class with byte constants | Clean alternative to enum for discriminator |
| 6 | Missing command validators | NotEmpty() on Id param | Consistent with other simple commands in codebase |

---

## Fixes

### Fix 1 (MEDIUM): Create ProviderSuspendedIntegrationEventHandler

**Problem**: Plan Section E says `ProviderSuspendedIntegrationEvent → ContentPlaces → Auto-suspend all provider's businesses`. No handler exists — suspended providers retain active businesses.

**Files**:
- CREATE: `ContentPlaces.Infrastructure/EventHandlers/ProviderSuspendedIntegrationEventHandler.cs`

**Implementation**:
```csharp
// INotificationHandler<IntegrationEventNotification<ProviderSuspendedIntegrationEvent>>
// 1. Query IBusinessRepository.GetByOwnerIdAsync(event.UserId)
// 2. For each approved/pending business → business.Suspend(reason: "Provider suspended")
// 3. Save via IUnitOfWork
// 4. Log count of suspended businesses
```

**Dependencies**: 
- `ProviderSuspendedIntegrationEvent` in `Accounts.Contracts/IntegrationEvents/`
- `Business.Suspend()` domain method already exists
- Need to verify/add `IBusinessRepository.GetByOwnerIdAsync()` or similar

---

### Fix 2 (MEDIUM): Create ReviewCreatedIntegrationEventHandler

**Problem**: Plan Section E says `ReviewCreatedIntegrationEvent → Social → ContentPlaces → Update Place/Business ratings`. No handler exists — ratings remain stale.

**Files**:
- CREATE: `ContentPlaces.Infrastructure/EventHandlers/ReviewCreatedIntegrationEventHandler.cs`

**Implementation**:
```csharp
// INotificationHandler<IntegrationEventNotification<ReviewCreatedIntegrationEvent>>
// 1. Determine entity type (Place or Business) from event
// 2. Query repository for entity
// 3. Call entity.UpdateRating(newAverageRating, newReviewCount)
// 4. Save via IUnitOfWork
```

**Dependencies**:
- Verify `ReviewCreatedIntegrationEvent` exists in `Social.Contracts/IntegrationEvents/`
- `Place.UpdateRating()` and `Business.UpdateRating()` domain methods already exist
- If Social module event doesn't exist yet → defer (document as future work)

**⚠️ Pre-check**: Search `Social.Contracts` for ReviewCreatedIntegrationEvent. If it doesn't exist, this fix becomes a documentation update only.

---

### Fix 3 (MEDIUM): Create BusinessMoreDocsRequestedDomainEventHandler + Integration Event

**Problem**: `BusinessMoreDocsRequestedDomainEvent` exists in Domain and is raised by `Business.RequestMoreDocs()`, but has NO infrastructure handler and NO corresponding integration event in Contracts. All other 11 Business domain events have handlers.

**Files**:
- CREATE: `ContentPlaces.Contracts/IntegrationEvents/BusinessMoreDocsRequestedIntegrationEvent.cs`
- CREATE: `ContentPlaces.Infrastructure/EventHandlers/BusinessMoreDocsRequestedDomainEventHandler.cs`
- MODIFY: `IntegrationEventTypeRegistry.cs` — register `content-places.business.more-docs-requested.v1`

**Implementation**: Follow the pattern of `BusinessRejectedDomainEventHandler` — construct integration event, write to outbox.

---

### Fix 4 (MEDIUM): Add Slug/OldSlug to PlaceUpdatedIntegrationEvent

**Problem**: `PlaceUpdatedIntegrationEvent` lacks `Slug` and `OldSlug` fields. ContentSeo and Analytics consumers cannot update sitemap URLs or generate 301 redirects when a place slug changes.

**Files**:
- MODIFY: `ContentPlaces.Contracts/IntegrationEvents/PlaceUpdatedIntegrationEvent.cs` — add `string Slug, string? OldSlug`
- MODIFY: `ContentPlaces.Infrastructure/EventHandlers/PlaceUpdatedDomainEventHandler.cs` — populate new fields
- MODIFY: ContentSeo/Analytics consumers (if they need to use the new fields)

---

### Fix 5 (LOW): Add Missing Command Validators

**Problem**: 7 commands lack validators: ApproveBusiness, DeleteBusiness, ReinstateBusiness, RequestMoreDocs, ResubmitBusiness, RemoveBusinessStaff, UpdateBusinessAccessibilityFeatures.

**Files**:
- CREATE: 7 validator files in `ContentPlaces.Application/Commands/` subdirectories

**Implementation**: Each follows `RuleFor(x => x.Id).NotEmpty()` or `RuleFor(x => x.BusinessId).NotEmpty()` pattern.

---

### Fix 6 (LOW): Add AccessibilityEntityType Constants

**Problem**: `AccessibilityFeature.EntityType` uses raw `byte` values (0, 1) without named constants. Magic numbers reduce readability.

**Files**:
- CREATE: `ContentPlaces.Domain/Constants/AccessibilityEntityType.cs`

**Implementation**:
```csharp
namespace ContentPlaces.Domain.Constants;

public static class AccessibilityEntityType
{
    public const byte Place = 0;
    public const byte Business = 1;
}
```

- MODIFY: Update all usages of hardcoded 0/1 in handlers and queries to use these constants

---

### Fix 7 (LOW): Fix BusinessEndpoints Route Prefix Duplication — ✅ FIXED

**Problem**: `BusinessEndpoints.cs` defined group prefix `/places/businesses` (line 43) but child routes re-specified full paths like `/places/businesses/{id:guid}`, producing doubled routes like `/api/v1/places/businesses/places/businesses/{id}`.

**Fix Applied**: Changed `group.MapGroup("/places/businesses")` → `group.MapGroup("")` (empty prefix, tag-only group). Updated 3 routes that were already relative (`/mine`, `/{id}/accessibility` × 2) to include full `/places/businesses/` prefix. All other routes already had full paths and now resolve correctly.

**Resulting routes** (all correct):
- `GET /api/v1/places/{id}/businesses` — list by place
- `GET /api/v1/places/businesses/{id}` — get by id
- `GET /api/v1/places/businesses/search` — search
- `POST /api/v1/places/businesses` — create
- `POST /api/v1/places/businesses/admin/{id}/approve` — admin approve
- `GET /api/v1/places/businesses/mine` — my businesses
- `GET /api/v1/places/businesses/{id}/accessibility` — accessibility

---

### Fix 8 (LOW): Update Plan Documentation

**Problem**: Plan has several documentation-only inaccuracies.

**File**: `ContentPlaces-Workflow.md`

**Changes**:
1. Line 4: Change `Status: Plan` → `Status: Implemented` with implementation date
2. Add `LanguageActivatedIntegrationEventHandler` to Section E cross-module table
3. Add `BusinessTranslation` entity mention in Section C or entities list
4. Section C.2: Fix discount field names — `ValidFrom, ValidTo` → `DiscountValidFrom, DiscountValidTo`
5. Add cross-module consumer map (ContentSeo, ContentBlogs, Analytics, Social, Messaging)
6. Add Implementation Notes section (extra Business properties, module scale, ProviderApproved mechanism, no background services)

---

### Fix 9 (MEDIUM): Build Verify

Run `dotnet build YallaJo.sln --no-restore` after code changes. Must pass with 0 errors.

---

## Execution Order

```
Fix 1 (ProviderSuspended handler) ──────────┐
Fix 2 (ReviewCreated handler) ──────────────┤
Fix 3 (MoreDocsRequested handler + event) ──┤
Fix 4 (PlaceUpdated Slug payload) ──────────┼──→ Fix 9 (Build)
Fix 5 (7 missing validators) ──────────────┤
Fix 6 (AccessibilityEntityType constants) ──┤
Fix 7 (Route prefix check) ────────────────┘
Fix 8 (Plan doc updates) ─── independent, no build gate
```

Fixes 1-7 are independent and parallelizable. Fix 9 gates on all code changes. Fix 8 is documentation-only.

---

## File Impact Summary

| Action | Count | Files |
|--------|-------|-------|
| CREATE | 10-11 | ProviderSuspendedHandler, ReviewCreatedHandler (if event exists), MoreDocsRequestedHandler, MoreDocsRequestedIntegrationEvent, 7 validators, AccessibilityEntityType.cs |
| MODIFY | 5-8 | PlaceUpdatedIntegrationEvent, PlaceUpdatedDomainEventHandler, IntegrationEventTypeRegistry, handlers/queries using hardcoded 0/1, ContentPlaces-Workflow.md, possibly BusinessEndpoints route |
| DELETE | 0 | — |
| **Total** | **15-19** | |

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| ReviewCreatedIntegrationEvent doesn't exist in Social.Contracts | Medium | Check first; defer if missing |
| Business.Suspend() requires reason parameter | Low | Check entity method signature |
| IBusinessRepository missing GetByOwnerIdAsync | Low | Already has GetByOwnerIdAsync per explore results |
| Hardcoded byte values used in multiple locations | Low | Search + replace with constants |
| PlaceUpdatedIntegrationEvent payload change breaks existing consumers | Medium | Add fields as new params (record positional), update consumers |
| BusinessEndpoints route prefix is actually correct (relative) | Medium | Verify at runtime before fixing |

---

## Follow-Up Items (Not in This Fix Plan)

- **BusinessStaff name-only entries**: Plan marks as "Future" — no action needed now
- **Staff task assignment**: Plan Section C.5 marks as future
- **Business dashboard**: Listed in Future Enhancements
- **Opening hours exceptions**: Listed in Future Enhancements
- **Business subscription tiers**: Property exists (`SubscriptionTier`) but no management endpoints
- **Query validators**: 11 missing query validators — lower priority than command validators
- **NearbyPlaceResult.cs**: Contains both `NearbyPlaceResult` and `NearbyBusinessResult` in one file — minor code org issue
