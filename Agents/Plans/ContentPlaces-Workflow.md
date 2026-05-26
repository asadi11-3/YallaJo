# ContentPlaces Workflow Plan

> Module: `ContentPlaces` — Geographic places & commercial businesses
> Status: Implemented (audited 2025-01-27)
> Dependencies: Accounts (ProviderApplication), ContentCore (Attachments, Tags), Booking (future ServiceItem→Reservation)

---

## Purpose

**Places** = Historical & tourism destinations (Petra, Dead Sea, Jerash, Amman Citadel)
**Businesses** = Commercial establishments LINKED to those places (restaurant near Petra, hotel by Dead Sea)
**ServiceItem** = Bookable services/products offered by businesses (menu items, spa treatments, rooms)
**BusinessStaff** = Future role-based task system for business employees

Discovery flow: Tourist visits Place → discovers nearby Businesses → browses ServiceItems → (future) books via Booking module.

---

## Design Decisions

| # | Decision | Value | Rationale |
|---|----------|-------|-----------|
| 1 | Business.PlaceId | **Required** (not nullable) | Forces all businesses into Place discovery flow. Creates network effect. |
| 2 | ProviderType → BusinessType | **Enforced mapping** | Prevents type misuse. Clear business identity. |
| 3 | Multi-business per place per provider | **Different types only** | Prevents duplicate listings. One restaurant per provider per place. |
| 4 | Business accessibility | **Yes, polymorphic** | Reuses existing AccessibilityFeature entity (EntityType=1 for Business) |
| 5 | Staff model | **Both options** | Staff can be platform users (UserId) OR name+role entries |
| 6 | Admin review queue | **Not needed** | Existing search with role-based visibility is sufficient |
| 7 | PlaceType expansion | **No** | Existing types (Shopping, Entertainment) cover non-historical areas |

---

## ProviderType → Allowed BusinessType Mapping

| ProviderType (Accounts) | Allowed BusinessTypes (ContentPlaces) |
|---|---|
| `TourOperator = 0` | `Agency = 3` |
| `IndependentGuide = 1` | `Guide = 5` |
| `HotelResort = 2` | `Hotel = 1` |
| `ActivityCenter = 3` | `Activity = 7` *(NEW)* |
| `Agency = 4` | `Agency = 3` |
| `BusinessOwner = 5` | `Restaurant = 0`, `Shop = 2`, `Transport = 4`, `Other = 6` |

> **Enforcement**: `CreateBusinessCommandHandler` validates mapping via `IProviderTypeReader` (Accounts.Contracts).
> If provider tries to create a non-allowed type → `Outcome.Forbidden` with `Business.TypeNotAllowedForProvider`.

---

## Unique Constraints

| Constraint | Columns | Filter | Purpose |
|---|---|---|---|
| Existing: LicenseNumber per type | `(LicenseNumber, BusinessType)` | `NOT NULL AND !IsDeleted` | No duplicate licenses |
| Existing: Place Name+Country | `(Name, Country)` | `NOT NULL AND !IsDeleted` | No duplicate places |
| **NEW**: One business per type per place per provider | `(PlaceId, BusinessType, OwnerId)` | `!IsDeleted` | Prevents duplicate listings |

---

## Complete Workflow

### A. Place Management (Admin-Only)

```
Admin creates Place (name, slug, type, lat/lng, city, country, meta)
  → PlaceCreatedDomainEvent
  → Auto-generates slug
  → Accessibility defaults to false

Admin can: Update, Delete (blocked if active businesses), Feature, Verify
Public can: List (paginated, filtered), Get by ID/slug, Nearby (Haversine), Map viewport
```

**PlaceType enum** (unchanged):
```
Attraction=0, Restaurant=1, Hotel=2, Shopping=3, Nature=4, Historical=5, Religious=6, Entertainment=7
```

### B. Business Lifecycle (Provider + Admin)

```
1. Provider approved (Accounts module)
     ↓
2. Provider calls POST /places/businesses
     ↓ Validations:
     - IProviderStatusService.IsApprovedProviderAsync() ← ALREADY IMPLEMENTED
     - Max 10 active businesses per provider ← ALREADY IMPLEMENTED
     - ProviderType → BusinessType mapping ← NEW
     - Unique (PlaceId, BusinessType, OwnerId) ← NEW
     - PlaceId exists (IPlaceExistenceService or repository) ← NEEDS ENFORCEMENT
     ↓
3. Business created as Status=Pending (auto)
     - 7 default BusinessHours (all closed)
     - SubmittedAt + ReviewDeadline (7 days) set
     - BusinessCreatedDomainEvent fired
     ↓
4. Admin reviews (within 7-day SLA)
     - Approve → Status=Approved, BusinessApprovedDomainEvent
     - Reject(reason) → Status=Rejected, BusinessRejectedDomainEvent
     - RequestMoreDocs(reason) → Status=MoreDocsNeeded, BusinessMoreDocsRequestedDomainEvent
     ↓
5a. If Rejected/MoreDocsNeeded:
     - Owner can Resubmit (max 3 times)
     - Status → Pending, new 7-day SLA
     ↓
5b. If Approved:
     - Business is LIVE
     - Owner configures: Hours, Amenities, ServiceItems, Accessibility, Staff
     - Admin can: Suspend(reason), Reinstate
     - Document expiry: 14-day grace period after expiry
```

**BusinessStatus enum**:
```
Pending=0, Approved=1, Rejected=2, Suspended=3, MoreDocsNeeded=4
```

**State machine**:
```
Create → Pending
Pending → Approved | Rejected | MoreDocsNeeded
MoreDocsNeeded → Approved | Rejected (admin) | Pending (resubmit)
Rejected → Pending (resubmit, max 3)
Approved → Suspended
Suspended → Approved (reinstate)
```

### C. Business Sub-Entities (Owner manages after approval)

#### C.1 BusinessHours
- Auto-created (7 days, all closed) on business creation
- Owner sets open/close times per day (max 2 entries per day for split shifts)
- `PUT /places/businesses/{id}/hours` — batch replace all hours

#### C.2 ServiceItem (Bookable services)
- Owner creates services: name, price, currency, category, duration, capacity
- Categories: Food, Room, Ticket, Spa, Rental, Activity, Other
- Has discount fields: DiscountPercent, SalePrice, ValidFrom, ValidTo
- Future: Booking module creates AvailabilitySlots from ServiceItems
- `CRUD` at `/places/businesses/{id}/services` and `/places/businesses/services/{id}`

#### C.3 BusinessAmenity
- Owner adds amenities: name, icon, sortOrder
- Public-facing (AllowAnonymous list)
- `GET/POST /places/businesses/{id}/amenities`, `DELETE /places/businesses/amenities/{id}`

#### C.4 AccessibilityFeature (Polymorphic)
- **EntityType=1**: Place accessibility (existing)
- **EntityType=2**: Business accessibility (NEW)
- Constants: `AccessibilityFeature.EntityTypePlace = 1`, `AccessibilityFeature.EntityTypeBusiness = 2`
- Same `AccessibilityFeatureType` enum: Wheelchair, Visual, Hearing, Cognitive, Mobility, Other
- Owner manages via `GET/PUT /places/businesses/{id}/accessibility`

#### C.5 BusinessStaff (Future Enhancement)
- Current: simple add/remove with UserId + Role
- Future: Both platform users (UserId linked) AND name-only entries
- Future: Role-based task assignment based on BusinessType
- `GET/POST /places/businesses/{id}/staff`, `DELETE /places/businesses/staff/{id}`

### D. Discovery (Public/Tourist)

| Endpoint | Purpose |
|---|---|
| `GET /places` | List places (paginated, filtered by category/rating/city/country/tours) |
| `GET /places/{id}` | Place detail (with tour count, rating) |
| `GET /places/{slug}` | Place detail by slug |
| `GET /places/nearby` | Haversine geo-search |
| `GET /places/map/viewport` | Bounding-box map pins |
| `GET /places/{id}/businesses` | Businesses at a place (Approved only for public) |
| `GET /places/businesses/{id}` | Business detail |
| `GET /places/businesses/search` | Text + type + city + country search |
| `GET /places/businesses/nearby` | Haversine geo-search for businesses |
| `GET /places/businesses/mine` | **NEW** — Provider's own businesses |

### E. Cross-Module Integration

| Event | Source | Consumer | Action |
|---|---|---|---|
| `ProviderApprovedIntegrationEvent` | Accounts | ContentPlaces | Unlocks business creation |
| `ProviderSuspendedIntegrationEvent` | Accounts | ContentPlaces | Auto-suspend all provider's businesses (ProviderSuspendedSuspendBusinessesHandler) |
| `BusinessApprovedDomainEvent` | ContentPlaces | Messaging | Notify provider |
| `PlaceTourCountUpdatedIntegrationEvent` | ContentTours | ContentPlaces | Update Place.TourCount (PlaceTourCountUpdatedIntegrationEventHandler) |
| `RatingRecalculatedIntegrationEvent` | Social | ContentPlaces | Update Place/Business ratings (RatingRecalculatedUpdateEntityHandler) |
| `LanguageActivatedIntegrationEvent` | ContentCore | ContentPlaces | Auto-translate entities (LanguageActivatedIntegrationEventHandler) |
| (Future) `ServiceItemCreatedEvent` | ContentPlaces | Booking | Enable slot creation |

---

## Code Changes Required

### Phase 1: Domain & Constraints (Low Risk)

| # | Change | File | Type |
|---|--------|------|------|
| 1.1 | Add `BusinessType.Activity = 7` | `ContentPlaces.Domain/Enums/BusinessType.cs` | Enum |
| 1.2 | Make `Business.PlaceId` non-nullable (`Guid`) | `Business.cs` | Domain |
| 1.3 | Update `Business.Create()` — PlaceId required param | `Business.cs` | Domain |
| 1.4 | Update `Business.Update()` — PlaceId required param | `Business.cs` | Domain |
| 1.5 | Add unique index `(PlaceId, BusinessType, OwnerId)` | `BusinessConfiguration.cs` | EF |
| 1.6 | Migration: `ALTER COLUMN PlaceId SET NOT NULL` | EF Migration | DB |

### Phase 2: Provider Type Mapping (Medium Risk)

| # | Change | File | Type |
|---|--------|------|------|
| 2.1 | Create `IProviderTypeReader` interface | `Accounts.Contracts/Abstractions/` | Contract |
| 2.2 | Implement `ProviderTypeReader` | `Accounts.Application/Services/` | Service |
| 2.3 | Register in DI | `Accounts.Application/DependencyInjection.cs` | DI |
| 2.4 | Add type mapping validation to `CreateBusinessCommandHandler` | `ContentPlaces.Application` | Handler |
| 2.5 | Add project reference to Accounts.Contracts (if not already) | `.csproj` | Config |

### Phase 3: Business Accessibility (Low Risk)

| # | Change | File | Type |
|---|--------|------|------|
| 3.1 | Define `AccessibilityEntityType` constants (Place=0, Business=1) | Domain constant | Domain |
| 3.2 | Create `GetBusinessAccessibilityFeaturesQuery` + handler | Application | Query |
| 3.3 | Create `UpdateBusinessAccessibilityFeaturesCommand` + handler | Application | Command |
| 3.4 | Add endpoints: `GET/PUT /places/businesses/{id}/accessibility` | Presentation | Endpoint |

### Phase 4: My Businesses Endpoint (Low Risk)

| # | Change | File | Type |
|---|--------|------|------|
| 4.1 | Create `GetMyBusinessesQuery` + handler | Application | Query |
| 4.2 | Add endpoint: `GET /places/businesses/mine` | Presentation | Endpoint |

### Phase 5: Constraint Enforcement in Existing Handlers (Medium Risk)

| # | Change | File | Type |
|---|--------|------|------|
| 5.1 | Validate PlaceId exists in `CreateBusinessCommandHandler` | Application | Handler |
| 5.2 | Validate unique (PlaceId, BusinessType, OwnerId) in handler | Application | Handler |
| 5.3 | Update `UpdateBusinessCommandHandler` — PlaceId non-nullable | Application | Handler |
| 5.4 | Update `CreateBusinessCommand` — PlaceId non-nullable | Application | Command |
| 5.5 | Update `CreateBusinessRequest` DTO — PlaceId required | Presentation | DTO |

### Phase 6: Solution Build + Test Fix

| # | Change | File | Type |
|---|--------|------|------|
| 6.1 | Fix any test files referencing nullable PlaceId | Tests | Test |
| 6.2 | Full solution build — 0 errors | - | Verification |

---

## File Inventory

**New files (~8-10):**
- `IProviderTypeReader.cs` (Accounts.Contracts)
- `ProviderTypeReader.cs` (Accounts.Application)
- `GetBusinessAccessibilityFeaturesQuery.cs` + handler
- `UpdateBusinessAccessibilityFeaturesCommand.cs` + handler
- `GetMyBusinessesQuery.cs` + handler

**Modified files (~10-12):**
- `BusinessType.cs` (add Activity=7)
- `Business.cs` (PlaceId non-nullable)
- `BusinessConfiguration.cs` (new unique index)
- `CreateBusinessCommandHandler.cs` (type mapping + PlaceId validation)
- `UpdateBusinessCommandHandler.cs` (PlaceId non-nullable)
- `CreateBusinessCommand.cs` (PlaceId non-nullable)
- `CreateBusinessRequest.cs` (PlaceId required)
- `BusinessEndpoints.cs` (add /mine + /accessibility)
- `Accounts.Application/DependencyInjection.cs` (register ProviderTypeReader)
- Test files (nullable PlaceId fixes)

**Total: ~18-22 files**

---

## Future Enhancements (Not in This Plan)

- **Staff task assignment**: Role-based tasks per BusinessType (waiter tasks vs receptionist tasks)
- **Business dashboard**: Provider dashboard with analytics, reviews, booking stats
- **ServiceItem → Booking bridge**: Booking module reads ServiceItems to create AvailabilitySlots
- **Business verification documents**: Like ProviderApplication docs but at business level
- **Business photos/gallery**: Via ContentCore Attachment system (EntityType.Business)
- **Opening hours exceptions**: Holiday hours, temporary closures
- **Business subscription tiers**: Premium listing, featured placement, priority in search

---

## Implementation Notes (from audit 2025-01-27)

1. **Actual module scale**: 254 .cs files (37 Domain, 131 Application, 20 Contracts, 47 Infrastructure, 19 Presentation)
2. **Entities not listed in plan**: `BusinessTranslation` (exists, used for multi-language business content)
3. **Extra Business entity properties**: `SubscriptionTier`, `IsHalal`, `HasVegetarianOptions`, `HasAlcoholFreeArea`
4. **Inbound event handlers (4)**: LanguageActivatedIntegrationEventHandler, PlaceTourCountUpdatedIntegrationEventHandler, ProviderSuspendedSuspendBusinessesHandler, RatingRecalculatedUpdateEntityHandler
5. **Domain event handlers (11)**: BusinessApproved/Created/Deleted/Reinstated/Rejected/Resubmitted/Suspended/Updated, PlaceCreated/Deleted/Updated
6. **Cross-module consumers**: ContentSeo (Place Created/Updated/Deleted), ContentBlogs (Place Deleted), Analytics (Place Created/Updated/Deleted), Social (Place Created/Updated/Deleted), Messaging (Business Approved/Rejected/Suspended/Reinstated)
7. **AccessibilityFeature EntityType values**: Place=1, Business=2 (NOT 0/1 as originally planned). Constants centralized on `AccessibilityFeature.EntityTypePlace` / `EntityTypeBusiness`
8. **BusinessMoreDocsRequestedDomainEvent**: Handler + `BusinessMoreDocsRequestedIntegrationEvent` added (W4-C). Registry entry: `content-places.business.more-docs-requested.v1`.
9. **ServiceItem discount fields**: Named `DiscountValidFrom`/`DiscountValidTo` (not `ValidFrom`/`ValidTo`)
10. **PlaceUpdatedIntegrationEvent**: Now includes `Slug` and `OldSlug` fields (W4-C) for ContentSeo 301 redirect support.
11. **7 validators added (W4-C)**: ApproveBusiness, DeleteBusiness, ReinstateBusiness, RequestMoreDocs, ResubmitBusiness, RemoveBusinessStaff, UpdateBusinessAccessibilityFeatures.
12. **BusinessEndpoints route prefix fix (W4-C)**: Changed `group.MapGroup("/places/businesses")` to `group.MapGroup("")` to prevent doubled route paths.
