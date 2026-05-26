# Platform Onboarding Workflow

> **Status**: Implemented (audited 2025-01-27, score 9.0/10). All 8 fixes applied. Source of truth for user journeys from sign-up to active platform participant.

---

## Table of Contents

1. [Design Decisions](#design-decisions)
2. [User Roles & Personas](#user-roles--personas)
 3. [Flow A: Guest Onboarding](#flow-a-guest-onboarding)
4. [Flow B: Provider Application](#flow-b-provider-application)
5. [Flow C: Post-Approval Automation](#flow-c-post-approval-automation)
 6. [Flow D: Guest → Provider Upgrade](#flow-d-guest--provider-upgrade)
7. [Flow E: Agency Guide Roster](#flow-e-agency-guide-roster)
8. [Flow F: Creator Application](#flow-f-creator-application)
9. [Provider Dashboard (Unified)](#provider-dashboard-unified)
10. [Entity Changes](#entity-changes)
11. [New Endpoints](#new-endpoints)
12. [Integration Events](#integration-events)
13. [Background Services](#background-services)
14. [Execution Phases](#execution-phases)

---

## Design Decisions

| # | Decision | Choice |
|---|----------|--------|
| 1 | BusinessOwner provider type | Add `BusinessOwner = 5` to ProviderType enum (restaurants, spas, shops, etc.) |
| 2 | Post-approval automation | Type-specific auto-creation (IndependentGuide → TourGuide profile, others → ready to create content) |
| 3 | Provider dashboard | Unified with type-specific sections at `/provider/dashboard` |
| 4 | Multi-type providers | One user, one provider type. Separate accounts if multiple roles needed |
| 5 | Creator vs Provider | Separate application flows. CreatorApplication independent from ProviderApplication |
| 6 | Agency guide roster | Both directions: agency invites guides OR guides apply to join agency |
| 7 | Document requirements | Keep hardcoded in entity for MVP. Configurable later if needed |
| 8 | Guest onboarding | Simple sign-up → verify email → browse → book. No application needed |
| 9 | Guest → Provider | Seamless upgrade. Same account gains provider role after approval |

---

## User Roles & Personas

### Role Hierarchy
```
Guest (default) → User (email verified)
  └─ Provider (approved ProviderApplication)
       ├─ TourOperator (creates tours, assigns guides)
       ├─ IndependentGuide (IS a tour guide, creates/runs tours)
       ├─ HotelResort (creates business in ContentPlaces)
       ├─ ActivityCenter (creates business in ContentPlaces)
       ├─ Agency (creates tours, manages affiliated guides)
       └─ BusinessOwner (creates business in ContentPlaces — restaurants, spas, etc.)
  └─ Creator (approved CreatorApplication, independent from Provider)
```

### Provider Type → Module Mapping

| ProviderType | Primary Module | Auto-Created On Approval |
|---|---|---|
| IndependentGuide | ContentTours | TourGuide profile (auto) |
| TourOperator | ContentTours | — (creates tours manually) |
| Agency | ContentTours | — (creates tours + manages guide roster) |
| HotelResort | ContentPlaces | — (creates Business manually) |
| ActivityCenter | ContentPlaces | — (creates Business manually) |
| BusinessOwner | ContentPlaces | — (creates Business manually) |

---

## Flow A: Guest Onboarding

```
User arrives → Register → Verify Email → Browse → Book
```

### Steps

1. **Register** (`POST /api/v1/auth/register`)
   - Email + Password + DisplayName
   - OR Social login (Google, Apple, Facebook)
   - Role assigned: `Guest` (default). Upgraded to `User` on email verification via `EmailVerifiedUpgradeRoleHandler`.

2. **Verify Email** (`POST /api/v1/auth/verify-email`)
   - 6-digit OTP sent to email
   - Account activated on verification
   - Can browse without verification but cannot book

3. **Complete Profile** (optional, `PUT /api/v1/profile`)
   - Avatar, phone, preferences (language, currency)
   - Better recommendations when profile complete

4. **Browse & Book**
   - Full access to tours, places, blogs
   - Can book tours, leave reviews (after booking), save favorites
   - No application or approval needed

### Validation Rules
- Email: unique, valid format
- Password: min 8 chars, 1 uppercase, 1 number, 1 special
- DisplayName: 2-50 chars, no profanity
- OTP: 6 digits, 10-min expiry, max 5 attempts

---

## Flow B: Provider Application

```
User → "Become a Provider" → Select Type → Fill Details → Upload Docs → Submit → Admin Review → Approved/Rejected
```

### Pre-conditions
- User must be a verified user (email confirmed, has `User` role)
- User must NOT have an existing active ProviderApplication (one per user)
- User must NOT already be an approved provider

### Steps

1. **Register Application** (`POST /api/v1/provider/register`)
   - Select ProviderType (one of 6 types)
   - Fill: BusinessName, ContactEmail, ContactPhone, Address, Description
   - TypeSpecificData (JSON, varies by type — e.g., guide certifications, hotel star rating)
   - Status → `Draft`
   - Domain event: `ProviderRegisteredDomainEvent`

2. **Upload Required Documents** (`POST /api/v1/provider/documents`)
   - Documents required depend on ProviderType (hardcoded):
     - **TourOperator**: BusinessLicense, TaxRegistration, TourismAuthorityLicense, InsuranceCertificate
     - **IndependentGuide**: GovernmentId, MotaLicense, TaxIdentificationNumber, InsuranceCertificate
     - **HotelResort**: BusinessLicense, TaxRegistration, ProofOfOwnership, HealthAndSafety, FireSafety
     - **ActivityCenter**: BusinessLicense, TaxRegistration, RelevantCertification, LiabilityInsurance, FireSafety
     - **Agency**: BusinessLicense, TaxRegistration, TourismAuthorityLicense, AffiliatedGuidesList, InsuranceCertificate
     - **BusinessOwner**: BusinessLicense, TaxRegistration, HealthAndSafety (if food/beverage)
   - Max 10 documents total
   - Supported formats: PDF, JPG, PNG (max 10MB each)

3. **Submit for Review** (`POST /api/v1/provider/apply`)
   - Validates ALL required documents uploaded
   - Status → `Pending`
   - Sets `SubmittedAt = DateTime.UtcNow`
   - Domain event: `ProviderApplicationSubmittedDomainEvent`
   - Integration event: `ProviderApplicationSubmittedIntegrationEvent` (for admin notification)

4. **Admin Review** (one of):
   - **Approve** (`POST /api/v1/admin/providers/{id}/approve`)
     - Status → `Approved`
     - Sets ReviewedAt, ReviewedByUserId
     - Domain event: `ProviderApprovedDomainEvent`
     - Integration event: `ProviderApprovedIntegrationEvent` → triggers post-approval automation
   - **Reject** (`POST /api/v1/admin/providers/{id}/reject`)
     - Status → `Rejected`
     - Stores RejectionReason
     - Sets CoolingPeriodEndsAt = now + 7 days
     - Integration event: `ProviderRejectedIntegrationEvent` (notification)
   - **Request More Docs** (`POST /api/v1/admin/providers/{id}/request-docs`)
     - Status → `MoreDocsNeeded`
     - Stores which doc types missing + admin notes
     - Integration event: `ProviderMoreDocsRequestedIntegrationEvent` (notification)

5. **Re-application** (if rejected):
   - Wait for 7-day cooling period
   - Max 3 re-applications total
   - Status reverts to `Draft`
   - ReapplicationCount incremented
   - Can update details + replace documents

### State Machine (Already Exists)
```
Draft → Submit → Pending → Approve → Approved → Suspend → Suspended → Reinstate → Approved
                         → Reject → Rejected → (7 days) → Draft (resubmit, max 3)
                         → RequestMoreDocs → MoreDocsNeeded → Submit → Pending
```

---

## Flow C: Post-Approval Automation

When `ProviderApprovedIntegrationEvent` fires, multiple consumers act based on `ProviderType`:

### All Types (Universal)
1. **Assign Provider Role** — User gets `Provider` role in Identity
2. **Send Notification** — "Your application has been approved!" (email + in-app)
3. **Link Creator Profile** (if exists) — existing behavior in ContentBlogs

### IndependentGuide Specific
4. **Auto-Create TourGuide Profile**
   - Consumer: `ProviderApprovedCreateTourGuideHandler` (in ContentTours)
   - Creates TourGuide with: UserId, DisplayName (from application), Bio (from application description)
   - Sets: IsActive = true, ApplicationId = providerApplication.Id
   - TrustTier = GuideTrustTier.New (default)
   - Status = TourGuideStatus.Active
   - Guide can immediately:
     - Update profile (add languages, specializations, avatar)
     - Apply to existing tours
     - Propose new tours (requires admin approval for New tier)

### TourOperator / Agency Specific
5. **Enable Tour Creation**
   - No auto-creation needed
   - Provider can immediately create tours (`POST /api/v1/tours`)
   - Tours start as Draft, must be submitted for admin review

### Agency Additional
6. **Enable Guide Roster Management**
   - Agency can invite existing IndependentGuides to affiliate
   - Agency can accept guide join requests

### HotelResort / ActivityCenter / BusinessOwner Specific
7. **Enable Business Creation**
   - Provider can create Business in ContentPlaces (`POST /api/v1/places/businesses`)
   - Business starts as Pending, admin approves
   - Max 10 active businesses per provider (already implemented)

### Event Handler Registry (New Handlers Needed)

| Handler | Module | Trigger | Action |
|---|---|---|---|
| `ProviderApprovedAssignRoleHandler` | Security | ProviderApproved | Assign Provider role to user |
| `ProviderApprovedCreateTourGuideHandler` | ContentTours | ProviderApproved (type=IndependentGuide) | Create TourGuide profile |
| `ProviderApprovedNotificationHandler` | Messaging | ProviderApproved | Send approval notification |
| `ProviderApprovedLinkCreatorProfileHandler` | ContentBlogs | ProviderApproved | Link existing CreatorProfile |

---

## Flow D: Guest → Provider Upgrade

```
Active User → "Become a Provider" → Standard Flow B → Approval → Same account, new role
```

### Key Points
- NO new account needed
- Existing bookings, favorites, reviews preserved
- Provider role ADDED (not replaced) — user can still book as user
- UI shows "Switch to Provider Dashboard" after approval
- Seamless — button in profile settings: "Become a Provider"

### Technical Flow
1. User clicks "Become a Provider" in app settings
2. Frontend navigates to provider registration form
3. `POST /api/v1/provider/register` — creates ProviderApplication linked to existing UserId
4. Standard document upload + submit flow
5. On approval: Identity service adds `Provider` role claim
6. Next login (or token refresh): JWT includes Provider role
7. UI shows provider dashboard option

### Guard: Already a Provider
- If user already has approved ProviderApplication → return error `Provider.AlreadyApproved`
- If user has pending application → return error `Provider.ApplicationPending`
- If user is in cooling period → return error `Provider.CoolingPeriodActive` with `CoolingPeriodEndsAt`

---

## Flow E: Agency Guide Roster

### Direction 1: Agency Invites Guide

```
Agency → Search Guides → Send Invitation → Guide Accepts/Declines → Affiliation Active
```

1. **Search Available Guides** (`GET /api/v1/agency/guides/available`)
   - Returns IndependentGuides NOT affiliated with another agency
   - Filterable: specialization, language, location, rating

2. **Send Invitation** (`POST /api/v1/agency/guides/invite`)
   - Agency sends to guide's UserId
   - Includes: message, proposed commission split
   - Status: Pending
   - 7-day expiry
   - Domain event: `AgencyInvitationSentDomainEvent`
   - Notification to guide

3. **Guide Responds**
   - Accept (`POST /api/v1/guides/me/invitations/{id}/accept`)
     - Creates `AgencyAffiliation` record
     - Guide now appears in agency's roster
     - Agency can assign guide to their tours
   - Decline (`POST /api/v1/guides/me/invitations/{id}/decline`)
     - Invitation marked Declined
     - No affiliation created

### Direction 2: Guide Applies to Agency

```
Guide → Browse Agencies → Apply → Agency Approves/Rejects → Affiliation Active
```

1. **Browse Agencies** (`GET /api/v1/agencies`)
   - Public list of approved agencies accepting guides
   - Shows: name, description, tour count, guide count, commission structure

2. **Apply to Agency** (`POST /api/v1/guides/agencies/{agencyUserId}/apply`)
   - Guide sends application with: message, why they want to join
   - Status: Pending
   - No expiry (agency reviews at their pace)
   - Notification to agency owner

3. **Agency Responds**
   - Approve (`POST /api/v1/agency/applications/{id}/approve`)
     - Creates `AgencyAffiliation` record
     - Same as invitation acceptance
   - Reject (`POST /api/v1/agency/applications/{id}/reject`)
     - With reason
     - Guide notified

### Affiliation Management

- **Remove Guide** (`DELETE /api/v1/agency/guides/{guideUserId}`)
  - Agency removes guide from roster
  - Active tour assignments preserved until tour date passes
  - Future assignments cancelled
  - Guide notified

- **Guide Leaves** (`DELETE /api/v1/guides/me/agency`)
  - Guide voluntarily leaves agency
  - Same behavior as removal
  - Note: Uses DELETE method (not POST) since it removes the affiliation

### Constraints
- One guide can be affiliated with ONLY ONE agency at a time
- IndependentGuide can operate independently AND be affiliated (they choose per-tour)
- Agency takes commission on tours assigned through them (defined in affiliation)
- Admin can override/audit any affiliation

---

## Flow F: Creator Application

> Independent from Provider. Separate flow with its own approval.

```
User → "Become a Creator" → Submit Application → Admin Review → Approved → Can Publish Blogs
```

### Already Built (Wave-7)
- `CreatorApplication` entity with full state machine
- `CreatorProfile` entity (DisplayName, Slug, Bio, Avatar, TrustTier, Stats)
- Application endpoints (create, submit, admin approve/reject/request-info)
- Creator invitation system (admin invites known creators)

### Connection to Provider
- A user can be BOTH a Provider and a Creator (separate applications, separate approvals)
- `CreatorProfile.LinkedProviderId` links to provider if applicable
- No auto-creation: provider must separately apply for creator if they want to blog

---

## Provider Dashboard (Unified)

Single dashboard at `/provider/dashboard` with type-specific sections.

### Universal Sections (All Provider Types)
| Section | Description |
|---|---|
| Overview | Status card, quick stats, pending actions |
| Documents | View/replace uploaded documents, expiry tracking |
| Notifications | In-app notifications (approval, reviews, bookings) |
| Account Settings | Business name, contact info, description |

### Tour Provider Sections (TourOperator, IndependentGuide, Agency)
| Section | Description |
|---|---|
| My Tours | List tours (Draft, Pending, Approved, etc.) with CRUD |
| Bookings | Incoming bookings, confirm/reject, calendar view |
| Calendar | Availability slots, blocked dates |
| Earnings | Revenue, commission, payout history |
| Reviews | Reviews on tours, respond |

### IndependentGuide Additional
| Section | Description |
|---|---|
| Guide Profile | Edit TourGuide profile (bio, languages, specializations) |
| Applications | My applications to other tours |
| Proposals | My proposed tours |
| Trust Tier | Current tier, progress, criteria |

### Agency Additional
| Section | Description |
|---|---|
| Guide Roster | Affiliated guides, invite/remove, applications |
| Guide Performance | Per-guide stats (bookings, ratings) |

### Business Provider Sections (HotelResort, ActivityCenter, BusinessOwner)
| Section | Description |
|---|---|
| My Businesses | List businesses with status |
| Services | Service items offered |
| Reservations | Incoming reservations (future) |
| Business Hours | Operating hours management |
| Reviews | Reviews on business |

---

## Entity Changes

### Modified Entities

**ProviderType enum** (`Accounts.Domain\Enums\ProviderType.cs`)
```csharp
public enum ProviderType
{
    TourOperator = 0,
    IndependentGuide = 1,
    HotelResort = 2,
    ActivityCenter = 3,
    Agency = 4,
    BusinessOwner = 5  // NEW
}
```

**ProviderApplication** — add required docs for BusinessOwner:
```csharp
{ ProviderType.BusinessOwner, new[] { DocumentType.BusinessLicense, DocumentType.TaxRegistration } }
```

### New Entities

**AgencyAffiliation** (`Accounts.Domain\Entities\AgencyAffiliation.cs`)
```
Properties:
  - Id (Guid)
  - AgencyUserId (Guid) — the agency provider's user ID
  - GuideUserId (Guid) — the affiliated guide's user ID
  - CommissionPercentage (decimal) — agency's cut
  - Status (AgencyAffiliationStatus: Active, Terminated)
  - JoinedAt (DateTime)
  - TerminatedAt (DateTime?)
  - TerminatedByUserId (Guid?) — who terminated
  - TerminationReason (string?)

Methods:
  - Create(agencyUserId, guideUserId, commissionPct)
  - Terminate(terminatedByUserId, reason)
```

**AgencyInvitation** (`Accounts.Domain\Entities\AgencyInvitation.cs`)
```
Properties:
  - Id (Guid)
  - AgencyUserId (Guid)
  - GuideUserId (Guid)
  - Message (string)
  - ProposedCommissionPercentage (decimal)
  - Status (InvitationStatus: Pending, Accepted, Declined, Expired)
  - ExpiresAt (DateTime) — 7 days from creation
  - RespondedAt (DateTime?)

Methods:
  - Create(agencyUserId, guideUserId, message, commissionPct)
  - Accept()
  - Decline()
  - Expire()
```

**AgencyApplication** (`Accounts.Domain\Entities\AgencyApplication.cs`)
```
Properties:
  - Id (Guid)
  - GuideUserId (Guid)
  - AgencyUserId (Guid)
  - Message (string)
  - Status (ApplicationStatus: Pending, Approved, Rejected)
  - RejectionReason (string?)
  - RespondedAt (DateTime?)

Methods:
  - Create(guideUserId, agencyUserId, message)
  - Approve()
  - Reject(reason)
```

### New Enums

```csharp
public enum AgencyAffiliationStatus { Active = 0, Terminated = 1 }
public enum AgencyInvitationStatus { Pending = 0, Accepted = 1, Declined = 2, Expired = 3 }
public enum AgencyApplicationStatus { Pending = 0, Approved = 1, Rejected = 2 }
```

---

## New Endpoints

### Agency Roster Management (8 endpoints)

> Routes are under `/api/v1/agency` (mounted in `AccountsEndpoints.cs` via `AgencyEndpoints` + `AgencyPublicEndpoints`).

| Method | Route | Auth | Handler |
|---|---|---|---|
| GET | `/api/v1/agency/guides` | Agency Owner | List affiliated guides |
| GET | `/api/v1/agency/guides/available` | Agency Owner | Search non-affiliated guides |
| POST | `/api/v1/agency/guides/invite` | Agency Owner | Invite guide |
| GET | `/api/v1/agency/invitations/sent` | Agency Owner | List sent invitations |
| POST | `/api/v1/agency/applications/{id}/approve` | Agency Owner | Approve guide application |
| POST | `/api/v1/agency/applications/{id}/reject` | Agency Owner | Reject guide application |
| GET | `/api/v1/agency/applications` | Agency Owner | List pending guide applications |
| DELETE | `/api/v1/agency/guides/{guideUserId}` | Agency Owner | Remove guide from roster |

### Guide ↔ Agency Endpoints (5 endpoints)

> Routes are under `/api/v1/guides` (mounted in `AccountsEndpoints.cs` via `GuideAgencyEndpoints`).

| Method | Route | Auth | Handler |
|---|---|---|---|
| GET | `/api/v1/guides/me/invitations` | Guide | List received invitations |
| POST | `/api/v1/guides/invitations/{id}/accept` | Guide | Accept invitation |
| POST | `/api/v1/guides/invitations/{id}/decline` | Guide | Decline invitation |
| POST | `/api/v1/guides/agencies/{agencyUserId}/apply` | Guide | Apply to agency |
| DELETE | `/api/v1/guides/me/agency` | Guide | Leave current agency |

### Agency Public (2 endpoints)

| Method | Route | Auth | Handler |
|---|---|---|---|
| GET | `/api/v1/agencies` | Anonymous | List agencies accepting guides |
| GET | `/api/v1/agencies/{id}` | Anonymous | Agency public profile |

### Provider Dashboard (4 endpoints — universal)

| Method | Route | Auth | Handler |
|---|---|---|---|
| GET | `/api/v1/provider/dashboard/overview` | Provider | Dashboard overview stats |
| GET | `/api/v1/provider/dashboard/notifications` | Provider | Recent notifications |
| GET | `/api/v1/provider/dashboard/pending-actions` | Provider | Actions requiring attention |
| GET | `/api/v1/provider/settings` | Provider | Provider settings |

**Total new endpoints: 19**

---

## Integration Events

### New Events to Publish

| Event | Published By | Consumed By |
|---|---|---|
| `ProviderApprovedIntegrationEvent` | Accounts | ContentTours (create guide), Messaging (notify), Security (assign role), ContentBlogs (link profile) |
| `ProviderSuspendedIntegrationEvent` | Accounts | ContentTours (deactivate guide), Booking (cancel future bookings), ContentPlaces (suspend businesses) |
| `AgencyAffiliationCreatedIntegrationEvent` | Accounts | ContentTours (enable guide assignment for agency tours) |
| `AgencyAffiliationTerminatedIntegrationEvent` | Accounts | ContentTours (revoke future assignments), Booking (handle active bookings) |

### Generic Status Changed Event
All provider state transitions (submitted, rejected, more-docs-requested, approved, suspended, reinstated) also publish a `ProviderStatusChangedIntegrationEvent` via `AccountsIntegrationConverters`. This generic event can be consumed by modules that only need to know "something changed" without handling each specific transition.

### Existing Events (Already Published)
- `ProviderStatusChangedIntegrationEvent` — generic event for ALL status transitions (can replace dedicated events for simple notification needs)
- `ProviderRejectedIntegrationEvent` — user notification (dedicated event, also fires alongside generic)
- `ProviderReinstatedIntegrationEvent` — re-enable downstream (dedicated event, also fires alongside generic)

> **Note**: `ProviderApplicationSubmitted` and `ProviderMoreDocsRequested` do NOT have dedicated integration event contracts. They publish via the generic `ProviderStatusChangedIntegrationEvent` only.

---

## Background Services

| Service | Module | Schedule | Action |
|---|---|---|---|
| `AgencyInvitationExpiryService` | Accounts | Every 1h | Expire invitations past 7-day deadline |
| `ProviderDocumentExpiryService` | Accounts | Daily | Check document expiry dates, notify 30 days before |

---

## Execution Phases

### Phase 1: ProviderType Extension (LOW RISK)
- Add `BusinessOwner = 5` to ProviderType enum
- Add required documents for BusinessOwner in ProviderApplication entity
- Update any validation/display logic
- **Files: ~3 modified**

### Phase 2: Post-Approval Automation (MEDIUM RISK)
- Create `ProviderApprovedCreateTourGuideHandler` in ContentTours
- Create `ProviderApprovedAssignRoleHandler` in Security
- Verify existing handlers still work (Messaging notification, ContentBlogs link)
- **Files: ~4-6 new, ~2 modified**

### Phase 3: Agency Roster — Domain (MEDIUM RISK)
- Create AgencyAffiliation, AgencyInvitation, AgencyApplication entities
- Create enums: AgencyAffiliationStatus, AgencyInvitationStatus, AgencyApplicationStatus
- Create repository interfaces
- Create EF configurations
- Register in DI
- **Files: ~12-15 new**

### Phase 4: Agency Roster — Application Layer (MEDIUM RISK)
- Create command handlers (invite, accept, decline, approve, reject, remove, leave, apply)
- Create query handlers (list guides, list invitations, list applications, available guides)
- Create validators
- **Files: ~20-25 new**

### Phase 5: Agency Roster — Endpoints (LOW RISK)
- Create AgencyEndpoints, GuideAgencyEndpoints
- Wire up permissions
- **Files: ~4-6 new**

### Phase 6: Provider Dashboard Queries (LOW RISK)
- Overview stats aggregation query
- Pending actions query
- Provider settings query/command
- **Files: ~8-10 new**

### Phase 7: Integration Event Handlers (MEDIUM RISK)
- AgencyAffiliationCreated/Terminated consumers in ContentTours + Booking
- Provider suspension cascade (already partially exists)
- **Files: ~4-6 new**

### Phase 8: Background Services (LOW RISK)
- AgencyInvitationExpiryService
- ProviderDocumentExpiryService
- **Files: ~2 new**

### Phase 9: Tests + Solution Build
- Unit tests for entities (state machine, validation)
- Integration tests for handlers
- **Files: ~10-15 new**

---

## File Count Summary

| Category | Count |
|---|---|
| New files | ~65-80 |
| Modified files | ~10-15 |
| Total | ~75-95 |

---

## Dependencies

| This Plan | Depends On |
|---|---|
| Phase 2 (TourGuide auto-creation) | TourGuide-Flow.md Part 2 (profile alignment) |
| Phase 3-5 (Agency roster) | TourGuide-Flow.md Part 1 (multi-guide model) |
| Provider Dashboard | Booking-Workflow.md (booking stats) |

---

## Implementation Notes (Post-Audit Additions)

> Added during codebase audit — reflects actual implementation details not originally in the plan.

1. **Role lifecycle**: `Guest` → `User` upgrade happens automatically via `EmailVerifiedUpgradeRoleHandler` when `EmailVerifiedIntegrationEvent` fires from `Security.Contracts` (not Auth.Contracts).
2. **External registration**: `RegisterExternalAsync()` (social login) assigns `User` role directly, bypassing the `Guest` → `User` lifecycle.
3. **No "Reinstated" status**: `Reinstate()` transitions to `Approved` (not a separate Reinstated status).
4. **AgencyInvitation.IsExpired**: Property exists on the entity for in-memory expiry checks (beyond the background service).
5. **Route group mounting**: `AccountsEndpoints.cs` mounts 5 top-level groups: `/api/v1/accounts`, `/api/v1/provider`, `/api/v1/admin/providers`, `/api/v1/agency`, `/api/v1/guides`.
6. **Reapplication state transition**: `Reapply()` domain method transitions `Rejected → Draft` with cooling-period and max-reapplication guards. Endpoint: `POST /api/v1/provider/reapply`.
7. **ProviderDocumentExpiryService**: Notification publishing is intentionally deferred (TODO) to Messaging-Workflow plan.
8. **CreatorApplicationApprovedIntegrationEvent**: Property is `ApplicantUserId` (not `UserId`), defined in `ContentBlogs.Contracts`.

---

## Future Extensions (NOT in MVP)

- [ ] Provider analytics dashboard (deep metrics)
- [ ] Provider subscription tiers (premium features)
- [ ] Automated document verification (AI/third-party)
- [ ] Provider rating/trust system (beyond guide tiers)
- [ ] Multi-language provider profiles
- [ ] Provider referral program
- [ ] Provider mobile app with push notifications
