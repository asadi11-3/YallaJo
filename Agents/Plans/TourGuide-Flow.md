# Tour Guide Flow — Architecture Plan

> **Status:** Implemented (audited 2025-01-27). All design decisions LOCKED.
> **Scope:** Extends ContentTours module with guide application system, tour proposals, multi-guide scheduling, and private tour variants.

### Implementation Notes (added by audit)
1. **ApplicationId** on TourGuide is `Guid?` (nullable), not `Guid` as plan specifies — allows guides created via agency affiliation path
2. **CommissionRate** was missing from TourGuide — added during audit fix (decimal?, precision 5,4)
3. **TourTourGuide** legacy entity still exists — active references in Assign/Unassign handlers prevent safe deletion. Deferred.
4. **Property naming differs from plan**: GuideApplication uses `Message` (not `QualificationSummary`), TourProposal uses `Title` (not `Name`), `RequestExclusive` (not `IsExclusive`), `CreatedTourId` (not `ApprovedTourId`)
5. **GuideTourOffering** timestamps `AssignedAt`/`SuspendedAt` were missing — added during audit fix
6. **GuideSchedule** missing `MaxGroupSize`, **GuidePricingTier** missing `ParticipantType` from plan specs
7. **GuideAvailabilityBlock.Create()** changed from throw to Result pattern during audit fix
8. **Domain events**: 6 events + 6 handlers added for GuideApplication (3) + TourProposal (3) during audit
9. **Integration event registry**: TourProposalSubmittedIntegrationEvent was missing — created during audit
10. **GuideOffering CRUD**: Full schedule/pricing/private-tour management endpoints (15 routes) created during audit
11. **Public/guide endpoints**: ListTourGuides, GetBySlug, MyApplications, GuideTours — created during audit
12. **Admin endpoints**: AdminUpdateTourGuide, AdminDeactivateTourGuide — created during audit
13. **11 validators** added for GuideApplication, TourProposal, GuideAvailabilityBlock, Suspend/Reinstate commands
14. Tour entity property is `Name` not `Title`; user ID property is `CreatedByUserId` not `AuthorId`

## Decision Summary

**Goal:** Transform Tour from a single-provider entity into a TEMPLATE that multiple guides can offer, each with their own schedule/pricing. Add guide application flow, tour proposals, and private tour variants.

### Core Design Decisions (LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | **Tour = Template** | Tour is a reusable template (e.g., 'Petra Day Tour'). Multiple guides can offer it. User books Guide + Tour + TimeSlot. |
| 2 | **Three creation paths** | Path A: Provider/Agency creates tour. Path B: Admin creates platform tour. Path C: Guide proposes tour. |
| 3 | **Guide application** | Guides submit DETAILED applications (why qualified, proposed schedule, pricing, experience, certifications). Agency tours: agency approves, admin can override. Platform tours: admin approves. |
| 4 | **Multi-guide per tour** | Same tour can have many guides (independent, NOT lead/assistant). Each creates own time slots/availability/pricing. Schedule-based, no hierarchy. |
| 5 | **Private tour variant** | Tours have BOTH 'shared' and 'private' options. Private = user books entire slot exclusively. Separate pricing per guide. |
| 6 | **Exclusive tours** | Guide-proposed tours only. PROPOSER DECIDES if exclusive or open. Exclusive = only proposer runs it. |
| 7 | **Shared ownership** | Guide-proposed tours have shared ownership (guide runs it, platform co-owns, different revenue cut). |
| 8 | **Trust/tier system** | Tiered privileges for guides. High tier: auto-approved proposals, more slots, priority in search, lower commission. Low tier: everything reviewed. Future expansion planned. |
| 9 | **IndependentGuide = TourGuide** | `ProviderType.IndependentGuide` IS a tour guide. One role, one application process. Can create own tours (Path C) or apply to existing ones. |
| 10 | **Agency assigns affiliates** | Agency assigns their affiliated guides directly. Freelance guides can also APPLY to run agency tours. Agency approves/rejects freelance applications, admin oversees. |
| 11 | **Booking model** | Booking linked to: Guide + Tour + TimeSlot (NOT just Tour). Different people book different guides at different times. |
| 12 | **PlaceId required** | ALL tours must be linked to an existing Place in ContentPlaces. Admin-created tours: PlaceId required. Guide proposals: PlaceId required (must reference existing Place). Provider tours: PlaceId required (already enforced by IPlaceExistenceService). |

---

## Current State

### Existing Entities (KEEP, MODIFY)
- **TourGuide** — `AuditableEntity, IAggregateRoot`. Props: UserId, Bio, YearsOfExperience, HasFirstAid, MoTALicenseNumber, AverageRating, ReviewCount, IsActive. Collections: Languages, Specializations.
- **Tour** — `AuditableEntity, IAggregateRoot`. Full aggregate with status machine (Draft→Pending→Approved/Rejected/Suspended/Archived). Props include CreatedByUserId, PlaceId, BasePrice, etc.
- **TourTourGuide** — Simple JOIN entity with TourId, TourGuideId, IsPrimary.
- **TourSchedule** — Belongs to Tour. DayOfWeek, StartTime, EndTime, IsActive.
- **TourPricingTier** — Belongs to Tour. Name, Price (Money), ParticipantType, Min/MaxParticipants.

### What Changes

**TourTourGuide** needs complete rework → becomes join + per-guide state/pricing/scheduling.
**TourSchedule** needs to become per-guide (not per-tour).
**TourPricingTier** needs per-guide variants.
**Tour** needs new fields for ownership model (ProposedByGuideId, IsExclusive, OwnershipType).

---

## New Entities

### 1. GuideApplication (NEW — `AuditableEntity, IAggregateRoot`)

Application from a guide to run an existing tour.

```
Properties:
  - Id (Guid)
  - TourId (Guid) — FK to Tour
  - GuideId (Guid) — FK to TourGuide
  - QualificationSummary (string, required, max 2000)
  - ProposedPricingNotes (string?, max 1000) — notes on their pricing approach
  - RelevantExperience (string?, max 2000)
  - CertificationsJson (string?) — JSON array of certification references
  - Status (GuideApplicationStatus enum)
  - ReviewedByUserId (Guid?)
  - ReviewedAt (DateTime?)
  - RejectionReason (string?)
  - AppliedAt (DateTime) — set on Create
  - SubmittedAt (DateTime?)

State machine:
  Draft → Submitted → Approved/Rejected
  Rejected → Resubmitted → Approved/Rejected (max 2 re-applications)

Domain Events:
  - GuideApplicationSubmittedDomainEvent(ApplicationId, TourId, GuideId)
  - GuideApplicationApprovedDomainEvent(ApplicationId, TourId, GuideId)
  - GuideApplicationRejectedDomainEvent(ApplicationId, TourId, GuideId, Reason)
```

### 2. GuideApplicationStatus (NEW enum)
```
Draft = 0
Submitted = 1
Approved = 2
Rejected = 3
```

### 3. TourProposal (NEW — `AuditableEntity, IAggregateRoot`)

Full tour proposal from a guide. If approved, creates a Tour + auto-assigns the proposing guide.

```
Properties:
  - Id (Guid)
  - GuideId (Guid) — FK to TourGuide (proposer)
  - Name (string, required)
  - Description (string, required)
  - ShortDescription (string?)
  - PlaceId (Guid) — REQUIRED, must reference existing Place in ContentPlaces (validated via IPlaceExistenceService)
  - Difficulty (TourDifficulty)
  - DurationMinutes (int)
  - MaxGroupSize (int)
  - ProposedBasePrice (decimal) — guide's proposed base price
  - Currency (string) — JOD/USD/EUR
  - IsExclusive (bool) — proposer decides: exclusive or open to other guides
  - ProposalNotes (string?, max 3000) — why this tour, target audience, etc.
  - Status (TourProposalStatus enum)
  - ReviewedByAdminId (Guid?)
  - ReviewedAt (DateTime?)
  - RejectionReason (string?)
  - ApprovedTourId (Guid?) — set when approved, FK to created Tour
  - SubmittedAt (DateTime)

State machine:
  Draft → Submitted → Approved/Rejected
  High-tier guides: Submitted → AutoApproved (trust-based)

Domain Events:
  - TourProposalSubmittedDomainEvent(ProposalId, GuideId)
  - TourProposalApprovedDomainEvent(ProposalId, GuideId, TourId)
  - TourProposalRejectedDomainEvent(ProposalId, GuideId, Reason)
```

### 4. TourProposalStatus (NEW enum)
```
Draft = 0
Submitted = 1
Approved = 2
Rejected = 3
```

### 5. GuideTourOffering (REPLACES TourTourGuide — `AuditableEntity`)

Per-guide configuration for a tour. This is the core entity connecting Guide to Tour with all per-guide state.

```
Properties:
  - Id (Guid)
  - TourId (Guid) — FK to Tour
  - GuideId (Guid) — FK to TourGuide
  - Status (GuideOfferingStatus enum)
  - OffersPrivateTour (bool, default false)
  - PrivateTourPriceMultiplier (decimal?, e.g. 2.0 = 2x base price)
  - PrivateTourFlatPrice (Money?) — alternative: flat price for private
  - IsProposer (bool, default false) — true if guide proposed this tour
  - ApplicationId (Guid?) — FK to GuideApplication (null if directly assigned)
  - AssignedByUserId (Guid?) — agency/admin who assigned (null if via application)
  - AssignedAt (DateTime)
  - SuspendedAt (DateTime?)
  - SuspendedReason (string?)

Methods:
  - Create() (factory)
  - Activate()
  - Suspend(string reason)
  - Reinstate()
  - SetPrivateTourPricing(decimal multiplier) or SetPrivateTourFlatPrice(Money price)
  - DisablePrivateTour()

State machine:
  Active → Suspended → Active (reinstate)
```

### 6. GuideOfferingStatus (NEW enum)
```
Active = 0
Suspended = 1
Removed = 2
```

### 7. GuideSchedule (NEW — `BaseEntity`)

Per-guide availability for a specific tour. Replaces per-tour TourSchedule for guide-specific scheduling.

```
Properties:
  - Id (Guid)
  - GuideTourOfferingId (Guid) — FK to GuideTourOffering
  - DayOfWeek (byte, 0-6)
  - StartTime (TimeOnly)
  - EndTime (TimeOnly?)
  - MaxGroupSize (int?) — guide's own max (override tour default)
  - IsActive (bool)

Methods:
  - Create(), Update(), Deactivate()
```

### 8. GuidePricingTier (NEW — `BaseEntity`)

Per-guide pricing for a specific tour. Each guide sets own pricing.

```
Properties:
  - Id (Guid)
  - GuideTourOfferingId (Guid) — FK to GuideTourOffering
  - Name (string)
  - Description (string?)
  - Price (Money)
  - ParticipantType (ParticipantType enum — reuse existing)
  - MinParticipants (int)
  - MaxParticipants (int)
  - IsActive (bool)

Methods:
  - Create(), Update(), Deactivate()
```

---

## Tour Entity Modifications

### Modified Properties on Tour
```csharp
// PlaceId: Guid? → Guid (REQUIRED — every tour must be linked to a Place)
// Migration: ALTER COLUMN PlaceId SET NOT NULL (all existing tours already have PlaceId set)
public Guid PlaceId { get; private set; }  // CHANGED from Guid? to Guid

// Ownership model (NEW)
public Guid? ProposedByGuideId { get; private set; }  // set if created via TourProposal
public bool IsExclusive { get; private set; }           // only proposer can run it
public TourOwnershipType OwnershipType { get; private set; }  // Provider, Platform, GuideProposed
public bool IsOpenForApplications { get; private set; }  // guides can apply to run this tour

// Navigation (existing TourTourGuide → replaced by GuideTourOffering)
public ICollection<GuideTourOffering> GuideOfferings { get; } = [];
```

### New Enum: TourOwnershipType
```
Provider = 0      // Created by agency/provider (Path A)
Platform = 1      // Created by admin (Path B)
GuideProposed = 2 // Created from guide proposal (Path C)
```

### PlaceId Constraint (MANDATORY for all paths)

**Rule:** Every tour MUST be associated with an existing Place in ContentPlaces.

| Creation Path | PlaceId Enforcement |
|---|---|
| Path A (Provider/Agency) | Already enforced by `IPlaceExistenceService` in CreateTourCommandHandler |
| Path B (Admin creates) | PlaceId is REQUIRED in CreateTourCommand. Validated via `IPlaceExistenceService` |
| Path C (Guide proposes) | PlaceId is REQUIRED in TourProposal entity. Validated at submission time via `IPlaceExistenceService` |

**Validation in handlers:**
- `CreateTourCommandHandler`: `placeExistenceService.ExistsAsync(command.PlaceId)` → 404 if not found (EXISTING)
- `SubmitTourProposalCommandHandler`: `placeExistenceService.ExistsAsync(proposal.PlaceId)` → returns error if Place doesn't exist (NEW)
- `ApproveTourProposalCommandHandler`: Re-validates PlaceId still exists before creating Tour from proposal (NEW)

**Why:** A tour is always AT a specific location. "Petra Day Tour" → Petra (Place). This ensures:
- Tours are discoverable by place/location
- Place-based search works (nearby tours)
- TourPlaceCountChangedDomainEvent fires correctly
- No orphan tours without a real-world location

### Existing TourSchedule / TourPricingTier
**KEEP as tour-level defaults.** When a guide is assigned, they can create GuideSchedule/GuidePricingTier that OVERRIDE the tour defaults. If a guide hasn't set custom schedule/pricing, fall back to tour-level defaults.

---

## TourGuide Entity Modifications

### New Properties
```csharp
public GuideTrustTier TrustTier { get; private set; }  // determines auto-approval privileges
public int CompletedTourCount { get; private set; }     // for tier promotion
public int ReportCount { get; private set; }            // for tier demotion/suspension
public decimal? CommissionRate { get; private set; }    // platform commission (tier-based)
```

### New Enum: GuideTrustTier
```
New = 0         // Everything reviewed, no auto-approval
Bronze = 1      // Some privileges
Silver = 2      // More privileges, lower commission
Gold = 3        // Auto-approved proposals, lowest commission, priority in search
```

### New Methods
```csharp
public void PromoteTier(GuideTrustTier newTier) { ... }
public void DemoteTier(GuideTrustTier newTier) { ... }
public void IncrementCompletedTours() { ... }
public void IncrementReportCount() { ... }
```

---

## Endpoints

### Guide Application Endpoints (NEW group: `/tours/{tourId}/applications`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/tours/{tourId}/applications` | Guide | Submit application to run a tour |
| `GET` | `/tours/{tourId}/applications` | Tour Owner/Admin | List applications for a tour |
| `GET` | `/tours/{tourId}/applications/{id}` | Applicant/Owner/Admin | Get application details |
| `PUT` | `/tours/{tourId}/applications/{id}` | Applicant | Update draft application |
| `POST` | `/tours/{tourId}/applications/{id}/submit` | Applicant | Submit application for review |
| `POST` | `/tours/{tourId}/applications/{id}/approve` | Tour Owner/Admin | Approve application |
| `POST` | `/tours/{tourId}/applications/{id}/reject` | Tour Owner/Admin | Reject application with reason |

### Tour Proposal Endpoints (NEW group: `/tours/proposals`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/tours/proposals` | Guide | Create tour proposal |
| `GET` | `/tours/proposals/my` | Guide | List my proposals |
| `GET` | `/tours/proposals/{id}` | Proposer/Admin | Get proposal details |
| `PUT` | `/tours/proposals/{id}` | Proposer | Update draft proposal |
| `POST` | `/tours/proposals/{id}/submit` | Proposer | Submit proposal for review |
| `POST` | `/tours/proposals/admin/{id}/approve` | Admin | Approve proposal (creates Tour) |
| `POST` | `/tours/proposals/admin/{id}/reject` | Admin | Reject proposal with reason |
| `GET` | `/tours/proposals/admin/queue` | Admin | List pending proposals |

### Guide Offering Endpoints (extends `/tours/{tourId}/guides`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/tours/{tourId}/guides` | Anonymous | List guides offering this tour (EXISTING, enhanced) |
| `GET` | `/tours/{tourId}/guides/{guideId}/schedule` | Anonymous | Get guide's schedule for this tour |
| `GET` | `/tours/{tourId}/guides/{guideId}/pricing` | Anonymous | Get guide's pricing tiers |
| `PUT` | `/tours/{tourId}/guides/{guideId}/schedule` | Guide (self) | Set/update own schedule |
| `PUT` | `/tours/{tourId}/guides/{guideId}/pricing` | Guide (self) | Set/update own pricing tiers |
| `PUT` | `/tours/{tourId}/guides/{guideId}/private-tour` | Guide (self) | Configure private tour pricing |
| `POST` | `/tours/{tourId}/guides/{guideId}/suspend` | Tour Owner/Admin | Suspend guide from tour |
| `POST` | `/tours/{tourId}/guides/{guideId}/reinstate` | Tour Owner/Admin | Reinstate suspended guide |

### Tour Modifications

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/tours/{id}/open-applications` | Tour Owner/Admin | Open tour for guide applications |
| `POST` | `/tours/{id}/close-applications` | Tour Owner/Admin | Close applications |

### Guide Profile Enhancements

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/{id}/tours` | Anonymous | List tours a guide offers |
| `GET` | `/guides/me/applications` | Guide | List my applications across tours |

---

## Permissions (NEW)

### ContentToursFeatures additions
```csharp
public const string GuideApplication = nameof(GuideApplication);
public const string TourProposal = nameof(TourProposal);
public const string GuideOffering = nameof(GuideOffering);
```

### ContentToursPermissionCatalog additions
```
GuideApplication.Create    — Submit application to run a tour
GuideApplication.View      — View applications (own or as reviewer)
GuideApplication.Update    — Update own draft application
GuideApplication.Approve   — Approve guide application (owner/admin)
GuideApplication.Reject    — Reject guide application (owner/admin)

TourProposal.Create        — Create tour proposal
TourProposal.View          — View proposals (own or admin)
TourProposal.Update        — Update own draft proposal
TourProposal.Approve       — Approve proposal (admin)
TourProposal.Reject        — Reject proposal (admin)

GuideOffering.View         — View guide offerings
GuideOffering.Update       — Update own schedule/pricing
GuideOffering.Suspend      — Suspend guide from tour (owner/admin)
GuideOffering.Reinstate    — Reinstate suspended guide (owner/admin)
```

---

## Repository Interfaces (NEW)

```csharp
public interface IGuideApplicationRepository : IRepository<GuideApplication, Guid>
{
    Task<GuideApplication?> GetWithDetailsAsync(Guid id, CancellationToken ct);
    Task<bool> HasPendingApplicationAsync(Guid tourId, Guid guideId, CancellationToken ct);
    Task<int> CountApplicationsAsync(Guid tourId, Guid guideId, CancellationToken ct);
}

public interface ITourProposalRepository : IRepository<TourProposal, Guid>
{
    Task<TourProposal?> GetWithDetailsAsync(Guid id, CancellationToken ct);
}

public interface IGuideTourOfferingRepository : IRepository<GuideTourOffering, Guid>
{
    Task<GuideTourOffering?> GetByTourAndGuideAsync(Guid tourId, Guid guideId, CancellationToken ct);
    Task<int> CountActiveOfferingsAsync(Guid guideId, CancellationToken ct);
    Task<IReadOnlyList<GuideTourOffering>> GetByTourAsync(Guid tourId, CancellationToken ct, bool activeOnly = true);
}
```

---

## EF Configuration (NEW files)

1. `GuideApplicationConfiguration.cs` — Standard setup, FK to Tour + TourGuide, indexes on (TourId, GuideId), (Status)
2. `TourProposalConfiguration.cs` — FK to TourGuide, index on (GuideId, Status)
3. `GuideTourOfferingConfiguration.cs` — FK to Tour + TourGuide, unique index on (TourId, GuideId), owned GuideSchedule + GuidePricingTier collections
4. `GuideScheduleConfiguration.cs` — FK to GuideTourOffering
5. `GuidePricingTierConfiguration.cs` — FK to GuideTourOffering, Money value object

---

## Migration from TourTourGuide

1. **Keep** existing `TourTourGuide` table temporarily
2. **Create** `GuideTourOffering` table
3. **Migrate** existing TourTourGuide records → GuideTourOffering (Status=Active, AssignedAt=now, IsProposer=false)
4. **Drop** TourTourGuide table
5. **Note:** No production data, so clean drop is acceptable. If prod data existed, would need proper migration.

---

## Execution Phases

### Phase 1: Domain Layer (Additive, Low Risk)
1. Add new enums: GuideApplicationStatus, TourProposalStatus, GuideOfferingStatus, TourOwnershipType, GuideTrustTier
2. Create GuideApplication entity
3. Create TourProposal entity
4. Create GuideTourOffering entity (replaces TourTourGuide)
5. Create GuideSchedule entity
6. Create GuidePricingTier entity
7. Add new properties to Tour (ProposedByGuideId, IsExclusive, OwnershipType, IsOpenForApplications, GuideOfferings)
8. Add new properties to TourGuide (TrustTier, CompletedTourCount, ReportCount, CommissionRate)
9. Create domain events for GuideApplication, TourProposal
10. Remove TourTourGuide entity

### Phase 2: Infrastructure Layer (Medium Risk)
1. Add EF configurations for all new entities
2. Add repositories for GuideApplication, TourProposal, GuideTourOffering
3. Update TourConfiguration (new nav properties)
4. Update TourGuideConfiguration (new properties)
5. Update DbContext (new DbSets)
6. Remove TourTourGuide configuration

### Phase 3: Application Layer — Guide Applications (Medium Risk)
1. Create GuideApplication commands: Create, Update, Submit, Approve, Reject
2. Create GuideApplication queries: List (per tour), Get, MyApplications
3. Add validators
4. Add cache keys

### Phase 4: Application Layer — Tour Proposals (Medium Risk)
1. Create TourProposal commands: Create, Update, Submit, Approve (creates Tour + GuideTourOffering), Reject
2. Create TourProposal queries: Get, MyProposals, AdminQueue
3. Add validators
4. Add cache keys
5. Handle auto-approval for high-tier guides

### Phase 5: Application Layer — Guide Offerings (Medium Risk)
1. Rework existing Assign/Unassign handlers → use GuideTourOffering
2. Create GuideSchedule CRUD handlers
3. Create GuidePricingTier CRUD handlers
4. Create private tour configuration handlers
5. Create Suspend/Reinstate handlers
6. Add OpenForApplications/CloseApplications tour commands

### Phase 6: Presentation Layer (Low Risk)
1. Add GuideApplication endpoints
2. Add TourProposal endpoints
3. Enhance existing TourGuide endpoints (schedule, pricing, private tour)
4. Add Tour modifications (open/close applications)
5. Update permissions catalog

### Phase 7: Cleanup & Build (Low Risk)
1. Remove TourTourGuide entity + config + old assign/unassign handlers
2. Update tests
3. Solution build
4. Verify 0 errors

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Booking module expects TourTourGuide | Check cross-module references before deleting. New booking flow must use GuideTourOffering. |
| Existing Assign/Unassign endpoints have consumers | Keep backward-compat during transition phase, deprecate after. |
| TourSchedule is currently per-tour | Keep as defaults, GuideSchedule overrides. No breaking change for existing queries. |
| Auto-approval for high-tier guide proposals | Start with all-review. Add auto-approval in Phase 4 behind tier check. |

---

## Part 2: TourGuide Profile Alignment (CreatorProfile Parity)

> **Design decisions LOCKED** (from user Q&A session).

### Decisions

| # | Decision | Detail |
|---|----------|--------|
| 12 | **No follow system** | Guides are found via tour search. No TourGuideFollow entity. |
| 13 | **Full public identity** | Add Slug, DisplayName, AvatarUrl, CoverImageUrl to TourGuide |
| 14 | **Core stats only** | CompletedTourCount, AverageRating, ReviewCount, ReportCount |
| 15 | **Result pattern fix** | Convert ALL TourGuide methods from `throw` to `Result` pattern (agent-context.md §2.3) |
| 16 | **Provider cross-link** | Add ApplicationId + LinkedProviderId to link guide to IndependentGuide provider application |

---

### TourGuide Entity — Final Shape After Alignment

```csharp
public sealed class TourGuide : AuditableEntity, IAggregateRoot
{
    // ─── Identity ───────────────────────────────────────────────────────
    public Guid UserId { get; private set; }
    public Guid ApplicationId { get; private set; }       // NEW — provider application that created this
    public string Slug { get; private set; }               // NEW — URL-friendly unique slug
    public string DisplayName { get; private set; }        // NEW — public display name
    public string? AvatarUrl { get; private set; }         // NEW — profile avatar
    public string? CoverImageUrl { get; private set; }     // NEW — profile cover image
    public string Bio { get; private set; }                // EXISTING

    // ─── Professional ───────────────────────────────────────────────────
    public int YearsOfExperience { get; private set; }     // EXISTING
    public bool HasFirstAid { get; private set; }          // EXISTING
    public string? MoTALicenseNumber { get; private set; } // EXISTING

    // ─── Trust & Status ─────────────────────────────────────────────────
    public GuideTrustTier TrustTier { get; private set; }         // NEW (already in TourGuide-Flow plan)
    public TourGuideStatus Status { get; private set; }           // NEW — replaces bool IsActive
    public string? SuspensionReason { get; private set; }         // NEW
    public Guid? SuspendedByAdminId { get; private set; }         // NEW
    public DateTime? SuspendedAt { get; private set; }            // NEW

    // ─── Stats ──────────────────────────────────────────────────────────
    public decimal AverageRating { get; private set; }     // EXISTING
    public int ReviewCount { get; private set; }           // EXISTING
    public int CompletedTourCount { get; private set; }    // NEW
    public int ReportCount { get; private set; }           // NEW

    // ─── Cross-link ─────────────────────────────────────────────────────
    public Guid? LinkedProviderId { get; private set; }    // NEW

    // ─── Collections (EXISTING) ─────────────────────────────────────────
    public IReadOnlyCollection<TourGuideLanguage> Languages { get; }
    public IReadOnlyCollection<TourGuideSpecialization> Specializations { get; }

    // ─── Factory (CHANGED: throw → Result) ──────────────────────────────
    public static Result<TourGuide> Register(
        Guid userId, Guid applicationId, string slug, string displayName,
        string bio, int yearsOfExperience, bool hasFirstAid,
        string? moTALicenseNumber, string? avatarUrl = null);

    // ─── Profile Management (ALL → Result pattern) ─────────────────────
    public Result UpdateProfile(string displayName, string? bio, int yearsOfExperience,
        bool hasFirstAid, string? moTALicenseNumber);
    public Result ChangeSlug(string newSlug);
    public Result UpdateAvatar(string? avatarUrl);
    public Result UpdateCoverImage(string? coverImageUrl);
    public Result AddLanguage(Guid languageId, string proficiency);
    public Result RemoveLanguage(Guid languageId);
    public Result AddSpecialization(Guid specializationId);
    public Result RemoveSpecialization(Guid specializationId);

    // ─── Status Management (NEW) ────────────────────────────────────────
    public Result Suspend(Guid adminId, string reason);
    public Result Reinstate(Guid adminId);
    public Result Deactivate();  // self-deactivate, 60-day hard delete

    // ─── Stats Mutation (called by domain event handlers) ───────────────
    public void IncrementCompletedTourCount();
    public void IncrementReportCount();
    public void LinkProvider(Guid providerId);
}
```

**Key changes from current TourGuide:**
1. `Register()` now returns `Result<TourGuide>` (not raw entity + throws)
2. ALL methods return `Result` (not void + throws)
3. `bool IsActive` → `TourGuideStatus Status` enum
4. Added: Slug, DisplayName, AvatarUrl, CoverImageUrl, ApplicationId
5. Added: Suspension tracking (reason, adminId, date) — mirrors CreatorProfile.Suspend/Reinstate
6. Added: CompletedTourCount, ReportCount stats
7. Added: LinkedProviderId cross-link
8. `EnsureActive()` → returns `Result.Failure(...)` instead of throwing

### New Enum: TourGuideStatus
```csharp
public enum TourGuideStatus : byte
{
    Active = 0,
    Suspended = 1,
    Deactivated = 2   // self-deactivated, 60-day hard delete
}
```

### GuideTrustTier (already defined in Phase 1)
```csharp
public enum GuideTrustTier : byte
{
    New = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3
}
```

---

### Endpoints — TourGuide Profile (Aligned with CreatorProfile)

#### Public Endpoints (Anonymous)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides` | Anonymous | List guides (paginated, searchable) |
| `GET` | `/guides/{slug}` | Anonymous | Public profile by slug |

#### Self-Management (Guide Owner)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me` | Guide | Get own profile with full details |
| `PUT` | `/guides/me` | Guide | Update own profile (displayName, bio, yearsOfExperience, etc.) |
| `PUT` | `/guides/me/avatar` | Guide | Update avatar URL |
| `PUT` | `/guides/me/cover-image` | Guide | Update cover image URL |
| `DELETE` | `/guides/me` | Guide | Self-deactivate (soft delete, 60-day hard delete) |

#### Admin Management

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/admin/{id}` | Admin | Full profile detail with private stats (reportCount, etc.) |
| `PUT` | `/guides/admin/{id}` | Admin | Edit ALL fields including trust tier |
| `DELETE` | `/guides/admin/{id}` | Admin | Admin soft delete (60-day hard delete) |
| `POST` | `/guides/admin/{id}/suspend` | Admin | Suspend guide with reason |
| `POST` | `/guides/admin/{id}/reinstate` | Admin | Reinstate suspended guide |

#### Existing Endpoints (KEEP, may modify)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| `GET` | `/guides/{id:guid}` | Anonymous | KEEP (by GUID, for internal use). Add slug variant. |
| `PUT` | `/guides/{id:guid}` | Guide | DEPRECATE in favor of `/guides/me` |
| `POST` | `/guides/{id}/languages` | Guide | KEEP |
| `DELETE` | `/guides/{id}/languages/{languageId}` | Guide | KEEP |
| `POST` | `/guides/{id}/specializations` | Guide | KEEP |

---

### Existing Handler Changes (Result Pattern Migration)

All 5 existing handlers need updating for Result pattern:

| Handler | Current Return | New Return |
|---------|---------------|------------|
| `UpdateTourGuideProfileCommand` | void (throws) | `ICommand` → Result |
| `AddTourGuideLanguageCommand` | void (throws) | `ICommand` → Result |
| `RemoveTourGuideLanguageCommand` | void (throws) | `ICommand` → Result |
| `AddTourGuideSpecializationCommand` | void (throws) | `ICommand` → Result |
| `RegisterTourGuideCommand` (if exists) | void (throws) | `ICommand<Guid>` → Result |

Handler pattern: remove try/catch around domain calls, use `Result.IsFailure` checks instead.

---

### New Handlers

| Handler | Type | Description |
|---------|------|-------------|
| `GetMyGuideProfileQuery` | Query | Get own profile (returns full DTO with private stats) |
| `GetGuideBySlugQuery` | Query | Public profile by slug |
| `ListGuidesQuery` | Query | Paginated guide listing (anonymous) |
| `UpdateGuideAvatarCommand` | Command | Update avatar URL |
| `UpdateGuideCoverImageCommand` | Command | Update cover image URL |
| `DeactivateGuideCommand` | Command | Self-deactivate |
| `SuspendGuideCommand` | Command | Admin suspend with reason |
| `ReinstateGuideCommand` | Command | Admin reinstate |
| `AdminUpdateGuideCommand` | Command | Admin edit all fields |
| `AdminDeleteGuideCommand` | Command | Admin soft delete |
| `AdminGetGuideQuery` | Query | Admin full detail with private stats |

---

### ITourGuideRepository Additions

```csharp
// ADD to existing interface:
Task<TourGuide?> GetBySlugAsync(string slug, CancellationToken ct = default, bool asNoTracking = true);
Task<bool> IsSlugTakenAsync(string slug, Guid? excludeGuideId = null, CancellationToken ct = default);
```

---

### EF Configuration Updates

`TourGuideConfiguration.cs` needs:
- `Slug`: `IsRequired()`, `HasMaxLength(100)`, unique index with `[IsDeleted] = 0` filter
- `DisplayName`: `IsRequired()`, `HasMaxLength(100)`
- `AvatarUrl`: `IsRequired(false)`, `HasMaxLength(500)`
- `CoverImageUrl`: `IsRequired(false)`, `HasMaxLength(500)`
- `Status`: `IsRequired()`, `HasConversion<byte>()`
- `TrustTier`: `IsRequired()`, `HasConversion<byte>()`
- `SuspensionReason`: `IsRequired(false)`, `HasMaxLength(500)`
- `SuspendedByAdminId`: `IsRequired(false)`
- `SuspendedAt`: `IsRequired(false)`
- `ApplicationId`: `IsRequired()`
- `LinkedProviderId`: `IsRequired(false)`
- `CompletedTourCount`: `IsRequired()`, `HasDefaultValue(0)`
- `ReportCount`: `IsRequired()`, `HasDefaultValue(0)`

---

### Background Services

1. **`GuideCleanupService`** — Daily, hard-deletes guide profiles where `Status = Deactivated` AND `DeletedAt + 60 days < now`. Cascades: remove guide offerings, anonymize tour history.

---

### Permissions (NEW)

```
TourGuide.View      — View guide profile (existing, no change)
TourGuide.Update    — Update own profile (existing, no change)
TourGuide.Manage    — Admin manage guide profiles (NEW)
TourGuide.Suspend   — Admin suspend guide (NEW)
TourGuide.Reinstate — Admin reinstate guide (NEW)
TourGuide.Delete    — Admin/self delete guide (NEW)
```

---

### Execution Phase (Profile Alignment)

> This can be done as a pre-phase BEFORE the main Tour Guide Flow phases, since it's foundational.

**Phase 0.1: Domain** — New enums (TourGuideStatus), modify TourGuide entity (add props, Result pattern)
**Phase 0.2: Infrastructure** — Update EF config, add repo methods (GetBySlug, IsSlugTaken)
**Phase 0.3: Application** — New handlers (11 above) + modify 5 existing handlers for Result pattern
**Phase 0.4: Presentation** — New endpoints (12 above) + update existing endpoint group
**Phase 0.5: Tests** — Update existing tests for Result pattern changes

---

## Part 3: Tour Guide Dashboard Flow

> **All design decisions LOCKED** (from user Q&A session).

### Dashboard Decisions

| # | Decision | Detail |
|---|----------|--------|
| 17 | **Landing view** | ALL: Stats KPI cards + active tours + calendar + inbox/notifications |
| 18 | **Earnings** | Full earnings dashboard — total, per-tour breakdown, commission, payout history |
| 19 | **Booking management** | Full — upcoming bookings, tourist details, check-in/no-show, cancellation |
| 20 | **Analytics** | Rich — booking trends, rating breakdown, popular tours, peak days, repeat rate, conversion |
| 21 | **Tour offering management** | Centralized hub — 'My Tours' tab with inline schedule/pricing/private tour editing |
| 22 | **Applications tracking** | Dedicated tab — list all applications with status, feedback, resubmit |
| 23 | **Proposals tracking** | Dedicated tab — list proposals with status, admin feedback, linked tour |
| 24 | **Trust tier progress** | Visible with clear criteria — current tier, next tier requirements, progress bars |
| 25 | **Slot blocking** | Yes — guide can block specific dates/times as unavailable (calendar blackout) |
| 26 | **Payout system** | Full — bank transfer + Jordanian mobile wallets (JoMoPay, Orange Money, Zain Cash) |
| 27 | **Payout cycle** | Auto weekly + manual on-demand (min threshold, e.g. 20 JOD) |
| 28 | **Messaging** | No in-app chat — guide sees tourist contact info (phone/email) from booking |
| 29 | **Notifications** | Full — in-app bell + email for critical events. No push notifications. |
| 30 | **Calendar** | Full detail — weekly/monthly with recurring availability, bookings overlay, blocked dates, free slots |
| 31 | **Reviews** | View + respond publicly to tourist reviews, star breakdown across tours |
| 32 | **Cancellation policy** | Tour-level only — no per-guide override |
| 33 | **Live tracking** | Deferred — no live GPS session for now. Future module. |

---

### Dashboard Sections

#### 1. Overview / Home (Landing)

KPI cards showing at-a-glance stats:

| Card | Data Source | Description |
|------|------------|-------------|
| **Upcoming Bookings** | Bookings module | Count of confirmed bookings in next 7 days |
| **Total Earnings (Month)** | Payouts module | Current month gross earnings |
| **Average Rating** | TourGuide.AverageRating | Star rating with review count |
| **Completed Tours** | TourGuide.CompletedTourCount | Lifetime total |
| **Trust Tier** | TourGuide.TrustTier | Current tier badge + progress to next |
| **Pending Applications** | GuideApplication count | Awaiting review |
| **Unread Notifications** | Notification count | Bell icon badge |

Below cards: Quick-access list of today's/this week's upcoming booked tours.

---

#### 2. My Tours (Tour Offering Management Hub)

Centralized management for all tours the guide offers.

**List view:** Each tour offering card shows:
- Tour name, image, status (Active/Suspended)
- Next upcoming booking date
- Rating for this specific tour
- Quick actions: Edit Schedule, Edit Pricing, Configure Private Tour

**Inline editing per offering:**

| Feature | Description |
|---------|-------------|
| **Schedule** | Set/edit GuideSchedule (recurring days + times) |
| **Date Blocking** | Block specific dates as unavailable (calendar blackout) |
| **Pricing** | Set/edit GuidePricingTier entries (Adult, Child, etc.) |
| **Private Tour** | Toggle `OffersPrivateTour`, set multiplier or flat price |
| **Group Size** | Override tour's MaxGroupSize per guide |

**Entities involved:**
- `GuideTourOffering` — the per-guide offering state
- `GuideSchedule` — recurring availability
- `GuidePricingTier` — per-guide pricing
- NEW: `GuideSlotBlock` — date-specific unavailability

---

#### 3. Calendar

Full calendar view (weekly/monthly toggle).

**Layers displayed:**
1. **Availability blocks** (green) — from GuideSchedule recurring config
2. **Booked slots** (blue) — confirmed bookings with tourist name + group size
3. **Blocked dates** (red/grey) — from GuideSlotBlock
4. **Free slots** (white/light) — available but unbooked

**Interactions:**
- Click empty slot → quick-block or view availability
- Click booked slot → expand to see booking details (tourist info, group size, tour name)
- Click blocked slot → unblock

---

#### 4. Bookings

Full booking management.

| Feature | Description |
|---------|-------------|
| **Upcoming** | List of future confirmed bookings, sorted by date |
| **Past** | Completed bookings history |
| **Booking detail** | Tourist name, email, phone, group size, tour, date/time, payment status, special requests |
| **Check-in** | Mark tourists as checked-in on tour day |
| **No-show** | Mark tourist as no-show (triggers policy) |
| **Cancellation** | View cancellation requests, process per tour's cancellation policy |

**Note:** Cancellation policy follows the tour-level setting (Decision #32). Guide cannot override.

---

#### 5. Earnings & Payouts

Full financial dashboard.

**Earnings Overview:**

| Metric | Description |
|--------|-------------|
| **Total Earned (All Time)** | Gross earnings across all tours |
| **This Month** | Current month breakdown |
| **Pending Payout** | Earned but not yet paid out |
| **Commission Deducted** | Platform commission total (tier-based rate) |
| **Net Earnings** | Total after commission |

**Per-Tour Breakdown:** Table showing each tour offering with bookings count, gross, commission, net.

**Payout System:**

| Feature | Description |
|---------|-------------|
| **Payment Methods** | Bank transfer (IBAN) + JoMoPay + Orange Money + Zain Cash |
| **Auto Weekly Payout** | Every Sunday — transfers completed tour earnings minus commission |
| **Manual On-Demand** | Guide requests early payout anytime. Minimum 20 JOD threshold. Processed within 3 business days. |
| **Payout History** | List of all payouts with date, amount, method, transaction reference, status |
| **Add/Edit Payment Method** | Guide manages their bank/wallet details |

**Cross-Module Integration (NO new entities — Finance owns all money per Finance-Workflow Decision #1):**

- `Finance.PaymentMethod` — Guide manages bank/wallet via Finance module endpoints (`/finance/payment-methods`)
- `Finance.Payout` — Weekly batch + on-demand payouts via Finance module (`/finance/payouts/provider`)
- `Finance.GuideEarning` — Per-booking earning records in Finance module
- Dashboard queries call Finance module read endpoints or cross-module query contracts
- PaymentMethodType enum (BankTransfer=0, JoMoPay=1, OrangeMoney=2, ZainCash=3) lives in Finance.Domain

---

#### 6. Analytics

Rich analytics dashboard.

| Metric | Type | Description |
|--------|------|-------------|
| **Booking Trends** | Line chart | Bookings per week/month over last 6 months |
| **Rating Breakdown** | Bar chart | Distribution of 1-5 star reviews |
| **Popular Tours** | Ranked list | Top tours by booking count |
| **Peak Days** | Heatmap | Busiest days of the week |
| **Repeat Customer Rate** | Percentage | % of tourists who booked again |
| **View-to-Booking Conversion** | Percentage | Profile views → confirmed bookings |
| **Revenue Trend** | Line chart | Earnings per month |
| **Cancellation Rate** | Percentage | % of bookings cancelled |

**Data source:** Computed from Bookings, Finance.GuideEarning (cross-module query), TourGuide stats, page view tracking.

**Note:** Analytics are read-only computed views. May use materialized views or background rollup service for performance.

---

#### 7. My Applications

Dedicated tab for guide's tour applications.

| Column | Description |
|--------|-------------|
| Tour Name | The tour applied to |
| Status | Draft / Submitted / Approved / Rejected |
| Applied Date | When submitted |
| Reviewer Feedback | Rejection reason if rejected |
| Actions | Edit (Draft), Resubmit (Rejected, max 2), View Details |

**Source:** `GuideApplication` entity from Part 1 of this plan.

---

#### 8. My Proposals

Dedicated tab for guide's tour proposals.

| Column | Description |
|--------|-------------|
| Tour Name | Proposed tour title |
| Status | Draft / Submitted / Approved / Rejected |
| Submitted Date | When submitted |
| Admin Feedback | Rejection reason if rejected |
| Linked Tour | Link to created Tour if approved |
| Actions | Edit (Draft), View Details |

**Source:** `TourProposal` entity from Part 1 of this plan.

---

#### 9. Reviews

View and respond to tourist reviews.

| Feature | Description |
|---------|-------------|
| **All Reviews** | List of reviews across all tours, newest first |
| **Star Breakdown** | 5-star distribution chart |
| **Per-Tour Filter** | Filter reviews by specific tour |
| **Respond** | Write public response to a review (one response per review) |
| **Report** | Flag inappropriate review for admin review |

**Cross-Module Integration (NO new entity — Social module handles review replies per Social-Workflow Decision #13):**

- Guide responds to reviews via `Social.Review.AddReply()` (existing pattern: owner + admin can reply)
- Dashboard queries Social module's `GET /reviews?entityType=TourGuide&entityId={guideId}` endpoint
- One reply per review, editable within 48h — enforced by Social module

---

#### 10. Trust Tier Progress

Visible tier status with clear promotion criteria.

| Display Element | Description |
|----------------|-------------|
| **Current Tier Badge** | New / Bronze / Silver / Gold with visual badge |
| **Next Tier** | What's the next tier and its requirements |
| **Progress Bars** | Per-criterion progress (e.g., "Completed Tours: 15/25") |
| **Commission Rate** | Current platform commission for this tier |

**Promotion Criteria (configurable, not hardcoded):**

| Tier | Completed Tours | Min Rating | Max Report Rate | Min Active Months |
|------|----------------|------------|-----------------|-------------------|
| Bronze | 10 | 3.5 | 15% | 2 |
| Silver | 25 | 4.0 | 10% | 6 |
| Gold | 50 | 4.5 | 5% | 12 |

**Note:** Criteria stored in config/database, not hardcoded. Background service evaluates eligibility periodically (like CreatorProfile tier promotion).

---

#### 11. Notifications

Full notification system (in-app + email).

**In-App:**
- Bell icon with unread count
- Notification dropdown / dedicated page
- Mark as read / mark all as read

**Notification Types:**

| Type | Trigger | Channel |
|------|---------|---------|
| `NewBooking` | Tourist books guide's tour | In-app + Email |
| `BookingCancelled` | Tourist cancels booking | In-app + Email |
| `ReviewReceived` | Tourist leaves review | In-app + Email |
| `ApplicationApproved` | Tour owner/admin approves application | In-app + Email |
| `ApplicationRejected` | Tour owner/admin rejects application | In-app + Email |
| `ProposalApproved` | Admin approves tour proposal | In-app + Email |
| `ProposalRejected` | Admin rejects tour proposal | In-app + Email |
| `TierPromoted` | Guide promoted to next tier | In-app + Email |
| `TierDemoted` | Guide demoted | In-app + Email |
| `PayoutCompleted` | Payout processed successfully | In-app + Email |
| `PayoutFailed` | Payout failed | In-app + Email |
| `ProfileSuspended` | Admin suspends profile | In-app + Email |
| `ProfileReinstated` | Admin reinstates profile | In-app + Email |
| `AdminMessage` | Admin sends direct message | In-app + Email |

**Cross-Module Integration (NO new entity — Messaging module handles all notifications per Messaging-Workflow Decision #3):**

- Use existing `Messaging.Notification` entity with 10 new TourGuide types (40-49):
  - NewBooking=40, BookingCancelled=41, ReviewReceived=42, ApplicationApproved=43, ApplicationRejected=44
  - ProposalApproved=45, ProposalRejected=46, TierPromoted=47, PayoutCompleted=48, AgencyAffiliationApproved=49
- Dashboard queries Messaging module's `GET /notifications?userId={guideUserId}` endpoint
- Guide notification preferences via Messaging module's `NotificationPreference` entity
- Email integration via Messaging module's `ITransactionalEmailSender` (deep-link back to dashboard)

---

### New Entity: GuideSlotBlock

Date-specific unavailability (calendar blackout).

```
GuideSlotBlock (BaseEntity)
  - Id, GuideTourOfferingId (FK)
  - BlockedDate (DateOnly)
  - Reason (string?, max 200)
  - CreatedAt

// Alternative: block across ALL offerings at once
GuideAvailabilityBlock (BaseEntity)
  - Id, GuideId (FK to TourGuide)
  - StartDate (DateOnly)
  - EndDate (DateOnly)
  - Reason (string?, max 200)
  - CreatedAt
```

**Decision:** Use `GuideAvailabilityBlock` (guide-level, not per-offering) — when a guide blocks dates, they're unavailable for ALL their tours. More intuitive.

---

### Dashboard Endpoints

#### Notification Endpoints (`/guides/me/notifications`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/notifications` | Guide | List notifications (paginated, filterable by type) |
| `GET` | `/guides/me/notifications/unread-count` | Guide | Get unread count (for badge) |
| `POST` | `/guides/me/notifications/{id}/read` | Guide | Mark single as read |
| `POST` | `/guides/me/notifications/read-all` | Guide | Mark all as read |

#### Availability Block Endpoints (`/guides/me/availability-blocks`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/availability-blocks` | Guide | List all blocks |
| `POST` | `/guides/me/availability-blocks` | Guide | Create block (date range + reason) |
| `DELETE` | `/guides/me/availability-blocks/{id}` | Guide | Remove block |

#### Earnings & Payout Endpoints (`/guides/me/earnings`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/earnings/summary` | Guide | Earnings overview (totals, this month, pending) |
| `GET` | `/guides/me/earnings/by-tour` | Guide | Per-tour breakdown |
| `GET` | `/guides/me/earnings/history` | Guide | Earnings history (paginated) |
| `GET` | `/guides/me/payouts` | Guide | Payout history |
| `POST` | `/guides/me/payouts/request` | Guide | Request manual payout |
| `GET` | `/guides/me/payment-methods` | Guide | List payment methods |
| `POST` | `/guides/me/payment-methods` | Guide | Add payment method |
| `PUT` | `/guides/me/payment-methods/{id}` | Guide | Update payment method |
| `DELETE` | `/guides/me/payment-methods/{id}` | Guide | Remove payment method |
| `POST` | `/guides/me/payment-methods/{id}/set-default` | Guide | Set as default |

#### Review Response Endpoints (`/guides/me/reviews`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/reviews` | Guide | List all reviews (paginated, filterable by tour) |
| `GET` | `/guides/me/reviews/stats` | Guide | Star breakdown, average, total count |
| `POST` | `/guides/me/reviews/{reviewId}/respond` | Guide | Post public response |
| `PUT` | `/guides/me/reviews/{reviewId}/respond` | Guide | Edit response (within 48h) |
| `POST` | `/guides/me/reviews/{reviewId}/report` | Guide | Report inappropriate review |

#### Analytics Endpoints (`/guides/me/analytics`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/analytics/overview` | Guide | KPI summary (bookings, revenue, rating, conversion) |
| `GET` | `/guides/me/analytics/booking-trends` | Guide | Booking count over time (weekly/monthly) |
| `GET` | `/guides/me/analytics/revenue-trends` | Guide | Revenue over time |
| `GET` | `/guides/me/analytics/popular-tours` | Guide | Top tours by booking count |
| `GET` | `/guides/me/analytics/peak-days` | Guide | Busiest days distribution |

#### Tier Progress Endpoint (`/guides/me/tier`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/tier` | Guide | Current tier, next tier criteria, progress |

---

### Dashboard Phase Execution

> These phases follow AFTER Part 1 (Tour Guide Flow) and Part 2 (Profile Alignment).

**Phase 8: Notifications** — Wire 10 new integration event handlers → Messaging module (NO new entity)
**Phase 9: Availability Blocking** — GuideAvailabilityBlock entity, calendar integration
**Phase 10: Earnings & Payouts** — Wire dashboard queries to Finance module read endpoints (NO new entities)
**Phase 11: Reviews** — Wire dashboard queries to Social module review endpoints (NO new entity)
**Phase 12: Analytics** — Read-only computed queries, rollup background service
**Phase 13: Tier Progress** — Tier criteria config, progress computation endpoint

---

## File Count Estimate (Full Plan: Profile + Flow + Dashboard)

| Layer | New Files | Modified Files |
|-------|-----------|----------------|
| Domain (entities, enums, events) | ~18 | ~8 |
| Infrastructure (config, repos, services) | ~12 | ~8 |
| Application (commands, queries, services) | ~60-75 | ~20 |
| Presentation (endpoints, DTOs) | ~12 | ~6 |
| Background Services | ~4 | ~2 |
| **Total** | **~106-121** | **~44** |

> **Note:** ~35 fewer files vs original estimate. 5 entities (GuideNotification, GuideReviewResponse, GuidePaymentMethod, GuidePayout, GuideEarning) replaced by cross-module integration with Messaging, Social, and Finance modules.

---

## Implementation Notes (Audit 2025-01-27)

> Added during codebase audit — reflects actual implementation details and fixes applied.

1. **Domain events added (W3-C)**: 6 domain events created for GuideApplication (Submitted/Approved/Rejected) and TourProposal (Submitted/Approved/Rejected). All 6 handlers publish corresponding integration events via outbox.
2. **TourProposalSubmittedIntegrationEvent**: Created in ContentTours.Contracts (was missing). Registry entry: `content-tours.tour-proposal.submitted.v1`.
3. **CommissionRate**: Added to TourGuide entity as `decimal?` with EF config `HasPrecision(5, 4)`.
4. **GuideTourOffering timestamps**: `AssignedAt` and `SuspendedAt` added to entity + EF config.
5. **GuideAvailabilityBlock.Create()**: Changed from throwing to `Result<GuideAvailabilityBlock>` pattern.
6. **11 validators added**: ApplyForTour, ApproveGuideApplication, RejectGuideApplication, CreateTourProposal, SubmitTourProposal, ApproveTourProposal, RejectTourProposal, CreateGuideAvailabilityBlock, DeleteGuideAvailabilityBlock, SuspendTourGuide, ReinstateTourGuide.
7. **GuideOffering CRUD**: Full schedule/pricing/private-tour management implemented — 11 commands, 4 queries, 15 endpoints in GuideOfferingEndpoints.cs. GuideScheduleRepository + GuidePricingTierRepository added.
8. **Public/guide endpoints**: ListTourGuides, GetTourGuideBySlug, GetMyGuideApplications, GetGuideTours — all implemented.
9. **Admin endpoints**: AdminUpdateTourGuide, AdminDeactivateTourGuide — both implemented.
10. **TourTourGuide legacy entity**: Still exists (deeply embedded in 3 handlers + 4 test files). Removal deferred — retargeting to GuideTourOffering changes semantics.
11. **GuideTourOfferingSuspendedIntegrationEvent**: Created in ContentTours.Contracts. Published by SuspendGuideOfferingCommandHandler. Consumed by Booking.Infrastructure to cancel affected bookings.
12. **Property name differences**: GuideApplication uses `Message` (not QualificationSummary), TourProposal uses `Title` (not Name), `RequestExclusive` (not IsExclusive).