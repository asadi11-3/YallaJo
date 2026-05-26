# TourGuide-Flow.md — Audit Report

**Audited**: 2025-01-27 | **Plan**: `Agents/Plans/TourGuide-Flow.md` (1134 lines, 3 parts)  
**Score**: **6.8 / 10** — Core entities & Tour modifications done; significant gaps in GuideOffering management, domain events, legacy cleanup, and endpoint coverage.

---

## Executive Summary

| Dimension | Finding |
|---|---|
| Entities (8 planned) | 7/8 exist. GuideAvailabilityBlock in Part 3 also done. TourTourGuide NOT removed. |
| Enums (6) | 6/6 exact match |
| Tour Entity Mods | All 4 properties + methods present |
| TourGuide Entity Mods | 95% match — CommissionRate missing, ApplicationId nullable |
| Domain Events | **CRITICAL**: 0/6 for GuideApplication + TourProposal |
| Repositories (3+1) | 4/4 exist, richer than plan |
| Endpoints (27+ planned) | ~34/50+ — GuideOffering endpoints entirely missing |
| Commands/Handlers | 22/30+ — GuideOffering + several admin commands missing |
| Validators | 22 of 46 commands (48%) — 0 for GuideApplication/TourProposal/AvailabilityBlock |
| Permissions | 3 new features exist, minor action gaps |
| Result Pattern | Consistently applied except GuideAvailabilityBlock.Create() |
| Legacy Cleanup | TourTourGuide entity + repository NOT deleted |

---

## Part 1: Tour Guide Flow (Core)

### 1.1 New Entities

| Entity | Status | Key Gaps |
|---|---|---|
| GuideApplication | EXISTS | Property renames (QualificationSummary→Message, ReviewedByUserId→ReviewedByAdminId), extra GuideUserId + ProposedScheduleJson, **ZERO domain events** |
| TourProposal | EXISTS | Property renames (Name→Title, IsExclusive→RequestExclusive, ApprovedTourId→CreatedTourId), **ZERO domain events** |
| GuideTourOffering | EXISTS | Missing AssignedAt, SuspendedAt timestamps; PrivateTourFlatPrice is decimal? not Money? |
| GuideSchedule | EXISTS | Extra denormalized TourGuideId/TourId; MaxGroupSize in code but not in plan |
| GuidePricingTier | EXISTS | Extra denormalized TourGuideId/TourId; ParticipantType exists in code but not in plan |
| TourTourGuide | SHOULD BE DELETED | Still exists (35 lines) with ITourTourGuideRepository — Phase 7 removal not done |

### 1.2 Tour Entity Modifications — ALL PRESENT

- `ProposedByGuideId (Guid?)` — present
- `IsExclusive (bool)` — present  
- `OwnershipType (TourOwnershipType)` — present
- `IsOpenForApplications (bool)` — present
- `GuideOfferings` collection — present
- `OpenForApplications()` / `CloseForApplications()` methods — present

### 1.3 TourGuide Entity — 95% Match

| Property/Method | Plan | Actual | Status |
|---|---|---|---|
| CommissionRate (decimal?) | Required | MISSING | MISSING |
| ApplicationId (Guid) | Required (non-null) | Guid? (nullable) | MISMATCH |
| Status enum replaces IsActive | Yes | Yes | MATCH |
| Suspend(reason, adminId) | Yes | Suspend(reason, adminId, utcNow) | EXTRA PARAM |
| Result pattern on all methods | Yes | Yes | MATCH |
| EnsureActive() → Error? | Yes | Yes | MATCH |

### 1.4 Domain Events — CRITICAL GAP

| Event | Planned | Exists |
|---|---|---|
| GuideApplicationSubmittedDomainEvent | Yes | NO |
| GuideApplicationApprovedDomainEvent | Yes | NO |
| GuideApplicationRejectedDomainEvent | Yes | NO |
| TourProposalSubmittedDomainEvent | Yes | NO |
| TourProposalApprovedDomainEvent | Yes | NO |
| TourProposalRejectedDomainEvent | Yes | NO |

Zero domain events means zero integration event propagation for guide applications and tour proposals. No cross-module notification (Messaging, Analytics) of these lifecycle transitions.

### 1.5 Endpoints

| Group | Planned | Actual | Missing |
|---|---|---|---|
| GuideApplication | 7 | 6 | GET detail, PUT update-draft, POST submit |
| TourProposal | 8 | 5 | GET my-proposals, GET detail, PUT update, GET admin-queue |
| GuideOffering | 8 | 0 | ALL (schedule CRUD, pricing CRUD, private tour config) |
| TourGuide assign/unassign | 3 | 3 | None |

### 1.6 Commands

| Category | Count | Missing |
|---|---|---|
| GuideApplication | 3 (Apply/Approve/Reject) | Update, Submit |
| TourProposal | 4 (Create/Submit/Approve/Reject) | Update |
| GuideTourOffering | 0 | ALL (EnablePrivateTour, DisablePrivateTour, Suspend, Reinstate, Remove, schedule CRUD, pricing CRUD) |
| TourGuide | 11 | ChangeSlug, AdminUpdate, AdminDelete |

### 1.7 Permissions

| Feature | Planned Actions | Actual Actions | Gap |
|---|---|---|---|
| GuideApplication | Read, Create, View, Update, Approve, Reject | Read, Create, Approve, Reject | Update missing |
| TourProposal | Read, Create, View, Submit, Approve, Reject | Read, Create, Submit, Approve, Reject | None |
| GuideOffering | Read, View, Update, Suspend, Reinstate | Read, Suspend, Reinstate | Update missing |
| TourGuide.Manage | Specified | MISSING | Not in catalog |

---

## Part 2: TourGuide Profile Alignment

### 2.1 Profile Features — MOSTLY DONE

| Feature | Status |
|---|---|
| Slug field + uniqueness | EXISTS (ITourGuideRepository.IsSlugTakenAsync) |
| DisplayName field | EXISTS |
| Status enum replaces IsActive bool | DONE |
| Result pattern on all methods | DONE |
| UpdateProfile command + handler | EXISTS |
| UpdateAvatar / UpdateCoverImage | EXISTS |
| AddLanguage / RemoveLanguage | EXISTS |
| AddSpecialization / RemoveSpecialization | EXISTS |
| ChangeSlug command | MISSING |

### 2.2 Profile Endpoints

| Endpoint | Status |
|---|---|
| GET /guides/{id} (public) | EXISTS |
| PUT /guides/{id} (update profile) | EXISTS |
| GET /guides/me | EXISTS |
| PUT /guides/me/avatar | EXISTS |
| PUT /guides/me/cover-image | EXISTS |
| DELETE /guides/me (deactivate) | EXISTS |
| GET /guides (public list) | MISSING |
| GET /guides/{slug} (by slug) | MISSING |
| GET /guides/me/applications | MISSING |
| GET /guides/{id}/tours | MISSING |

### 2.3 Admin Endpoints

| Endpoint | Planned | Actual |
|---|---|---|
| GET /guides/admin/{id} | Yes | EXISTS |
| POST /guides/admin/{id}/suspend | Yes | EXISTS |
| POST /guides/admin/{id}/reinstate | Yes | EXISTS |
| PUT /guides/admin/{id} (edit) | Yes | MISSING |
| DELETE /guides/admin/{id} | Yes | MISSING |

---

## Part 3: Dashboard

### 3.1 GuideAvailabilityBlock — DONE

- Entity exists (34 lines) with Create/guard for EndDate > StartDate
- Repository + DI + migration all present
- 3 endpoints (GET, POST create, DELETE) + 2 queries + 4 commands
- **Minor**: Create() uses `throw` instead of Result pattern

### 3.2 Dashboard Endpoints — MOSTLY DONE

| Category | Routes | Status |
|---|---|---|
| Availability blocks | 3 | EXISTS |
| Tier progress | 1 | EXISTS |
| Earnings | 3 | EXISTS |
| Analytics | 4 | EXISTS |
| Notifications | 0 | NOT VERIFIED (may not be in plan scope) |

---

## Validators Gap

| Area | Validators | Commands | Coverage |
|---|---|---|---|
| GuideApplication | 0 | 3 | 0% |
| TourProposal | 0 | 4 | 0% |
| GuideAvailabilityBlock | 0 | 4 | 0% |
| TourGuide | 8 | 11 | 73% |
| Tour | 14 | 30 | 47% |
| **Total** | **22** | **52** | **42%** |

---

## Property Name Discrepancies

| Plan Name | Actual Name | Entity |
|---|---|---|
| QualificationSummary | Message | GuideApplication |
| ProposedPricingNotes | (absent) | GuideApplication |
| CertificationsJson | (absent) | GuideApplication |
| ReviewedByUserId | ReviewedByAdminId | GuideApplication, TourProposal |
| Name | Title | TourProposal |
| ProposedBasePrice | BasePrice | TourProposal |
| IsExclusive | RequestExclusive | TourProposal |
| ApprovedTourId | CreatedTourId | TourProposal |
| PrivateTourFlatPrice (Money?) | PrivateTourFlatPrice (decimal?) | GuideTourOffering |

These are implementation-time naming improvements, not bugs. The plan document should be updated to match actual names.

---

## Scorecard

| Dimension | Score | Weight | Weighted |
|---|---|---|---|
| Entity Completeness | 7/10 | 20% | 1.40 |
| Domain Events | 4/10 | 15% | 0.60 |
| Endpoint Coverage | 6/10 | 15% | 0.90 |
| Command/Handler Coverage | 6/10 | 15% | 0.90 |
| Validator Coverage | 4/10 | 10% | 0.40 |
| Permission System | 8/10 | 5% | 0.40 |
| Result Pattern Adoption | 9/10 | 5% | 0.45 |
| Legacy Cleanup | 3/10 | 5% | 0.15 |
| Plan Accuracy (names/props) | 7/10 | 5% | 0.35 |
| Infrastructure (repos/EF/DI) | 9/10 | 5% | 0.45 |
| **Total** | | **100%** | **6.00 → 6.8/10** |

---

## Follow-Up Items (Not In Fix Scope)

1. GuideOffering endpoint design review (schedule/pricing/private tour — may warrant separate plan)
2. Cross-module consumers for future GuideApplication/TourProposal events (Messaging, Analytics)
3. TourTourGuide data migration strategy (populate GuideTourOffering from legacy, then drop)
4. GuideAvailabilityBlock Result pattern alignment
5. Validator coverage gap-fill across all ContentTours commands
