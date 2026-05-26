# Platform-Onboarding-Workflow — Audit Report

> **Date**: 2025-07-18
> **Scope**: Full verification of `Platform-Onboarding-Workflow.md` against actual codebase
> **Method**: 3 parallel explore agents + extensive direct file reads/searches
> **Verdict**: 7.8/10 — Substantially implemented with 1 critical bug, 3 missing endpoints, route naming drift, and event contract inaccuracies

---

## Executive Summary

| Category | Count |
|----------|-------|
| Design Decisions Correct | 9 of 9 |
| Flows Fully Implemented | 4 of 6 (A, C, E, F) |
| Flows Partially Implemented | 2 of 6 (B, D) |
| Bugs Found | 1 CRITICAL, 0 HIGH |
| Missing Endpoints | 3 of 19 claimed |
| Route Naming Mismatches | 13 endpoints |
| Integration Event Discrepancies | 2 events use generic contract, not dedicated |
| Documentation Inaccuracies | 9 |
| Background Services | 2/2 exist, 1 incomplete |

The Platform-Onboarding plan is **substantially implemented**. All 6 provider types, the full agency roster system (3 entities, 3 enums, repos, EF configs, migration), all 4 post-approval handlers, and both background services exist. The **critical gap** is a state machine bug preventing rejected providers from reapplying. Secondary issues are 3 missing endpoints, pervasive route naming drift between plan and code, and 2 integration events that don't exist as separate contracts.

---

## Section-by-Section Verification

### Section 1: Design Decisions — ✅ ALL 9 CORRECT

All design decisions are implemented as described. No contradictions found.

### Section 2: Role Naming — ❌ INACCURATE

| Claim | Plan | Actual | Status |
|-------|------|--------|--------|
| Default role name | "Tourist" | Guest | ❌ No "Tourist" role exists |
| Post-email-verify role | "Tourist → User" | Guest → User | ❌ Naming only |
| EmailVerifiedUpgradeRoleHandler | Exists | Confirmed in Security.Infrastructure | ✅ |

**Impact**: Plan consistently uses "Tourist" (14+ occurrences) throughout Flows A, D. No such role exists in `AppRoles.cs`. The correct lifecycle is **Guest → User** (via email verification) → Provider/Creator/TourGuide (via approval).

### Section 3: Flow A — Tourist Onboarding — ✅ IMPLEMENTED (naming drift)

| Claim | Plan | Actual | Status |
|-------|------|--------|--------|
| POST /api/v1/auth/register | ✅ | Exists | ✅ |
| POST /api/v1/auth/verify-email | ✅ | Exists | ✅ |
| OTP: 6 digits, 10min, 5 attempts | ✅ | Confirmed in EmailVerificationService | ✅ |
| Default role: Tourist | Tourist | Guest | ❌ Name only |
| Guest → User on email verify | ✅ | EmailVerifiedUpgradeRoleHandler | ✅ |

### Section 4: Flow B — Provider Application — ⚠️ PARTIALLY IMPLEMENTED

| Claim | Plan | Actual | Status |
|-------|------|--------|--------|
| ProviderType enum (6 types) | ✅ | EXACT match (0-5) | ✅ |
| ProviderApplication entity | ✅ | Full entity with state machine | ✅ |
| 7 domain events raised | ✅ | Registered, Submitted, Approved, Rejected, MoreDocsRequested, Suspended, Reinstated | ✅ |
| Document requirements per type | ✅ | EXACT match for all 6 types | ✅ |
| MaxReapplications=3, CoolingPeriodDays=7 | ✅ | Private constants in entity | ✅ |
| Rejected → Draft reapplication | After 7 days, max 3 | **NOT IMPLEMENTED** — Submit() blocks Rejected state | 🔴 CRITICAL |
| ProviderApplicationStatus enum | 6 values | Draft, Pending, MoreDocsNeeded, Approved, Rejected, Suspended | ✅ |
| No "Reinstated" status | — | Reinstate() returns to Approved | ✅ (correct design) |
| POST /api/v1/provider/register | ✅ | Exists | ✅ |
| POST /api/v1/provider/documents | ✅ | Exists (+ PUT for replace) | ✅ |
| POST /api/v1/provider/apply | ✅ | Exists | ✅ |
| Admin: approve/reject/request-docs/suspend/reinstate | ✅ | All exist under /api/v1/admin/providers/ | ✅ |

#### 🔴 CRITICAL BUG: Reapplication State Transition Missing

**Plan specification** (Section 4, Step 6): "Rejected → (wait 7-day cooling period) → Draft → Submit again. Max 3 re-applications."

**Actual code**: `ProviderApplication.Submit()` checks `Status is not (ProviderApplicationStatus.Draft or ProviderApplicationStatus.MoreDocsNeeded)` and throws if another state. `Rejected` is NOT in the allowed set.

**Evidence**:
- `CoolingPeriodEndsAt` IS set during `Reject()` method
- `ReapplicationCount` IS incremented during `Reject()` method
- `MaxReapplications` constant IS defined (3)
- But no method exists to transition from Rejected → Draft after cooling period
- Unit tests in `ProviderApplicationTests.cs` likely expect this to work

**Required fix**: Add `Reapply()` method or extend `Submit()` to accept Rejected state with cooling-period + max-reapplication guards.

### Section 5: Flow C — Post-Approval Automation — ✅ FULLY IMPLEMENTED

| Handler | Location | Behavior | Status |
|---------|----------|----------|--------|
| ProviderApprovedAssignRoleHandler | Security.Infrastructure | IndependentGuide → TourGuide; others → Provider | ✅ EXACT |
| ProviderApprovedCreateTourGuideHandler | ContentTours.Infrastructure | Creates TourGuide entity for IndependentGuide only | ✅ EXACT |
| ProviderApprovedNotificationHandler | Messaging.Infrastructure | In-app + optional email | ✅ |
| ProviderApprovedLinkCreatorProfileHandler | ContentBlogs.Infrastructure | Links LinkedProviderId on CreatorProfile | ✅ |

### Section 6: Flow D — Tourist → Provider Upgrade — ⚠️ PARTIALLY VERIFIABLE

The upgrade flow uses the same provider application pipeline. Guards (AlreadyApproved, ApplicationPending, CoolingPeriodActive) are embedded in entity state machine methods. Plan is directionally correct but the "Tourist" naming issue persists.

### Section 7: Flow E — Agency Guide Roster — ✅ FULLY IMPLEMENTED

**Entities** (all 3 confirmed):
| Entity | Exists | Properties Match | Status |
|--------|--------|-----------------|--------|
| AgencyAffiliation | ✅ | ✅ except `TerminatedBy` → actual: `TerminatedByUserId` | ⚠️ |
| AgencyInvitation | ✅ | ✅ + extra `IsExpired` property (not in plan) | ✅ |
| AgencyApplication | ✅ | ✅ | ✅ |

**Enums** (all 3 confirmed): AffiliationStatus, InvitationStatus, AgencyApplicationStatus — EXACT match.

**Repositories**: All 3 interfaces + implementations + DI registrations ✅  
**EF Configs**: All 3 configurations + migration (20260524221257_AddAgencyEntities) ✅  
**AgencyAffiliationReadService**: Exists ✅

### Section 8: Flow F — Creator Application — ✅ IMPLEMENTED

- CreatorApplication entity in ContentBlogs.Domain ✅
- CreatorProfile with LinkedProviderId ✅
- CreatorApplicationApprovedIntegrationEvent in ContentBlogs.Contracts ✅

### Section 9: Entity Changes — ✅ MOSTLY ACCURATE

All entity structures match with minor property naming differences noted above.

---

## Endpoint Verification

### Claimed: 19 new endpoints. Actual: 16 exist, 3 missing.

#### Agency Roster Management (8/8 exist — routes differ)

| # | Plan Route | Actual Route | Status |
|---|-----------|-------------|--------|
| 1 | POST /api/v1/provider/agency/invitations | POST /api/v1/agency/invitations | ❌ Route |
| 2 | GET /api/v1/provider/agency/invitations | GET /api/v1/agency/invitations | ❌ Route |
| 3 | POST /api/v1/provider/agency/invitations/{id}/cancel | POST /api/v1/agency/invitations/{id}/cancel | ❌ Route |
| 4 | GET /api/v1/provider/agency/members | GET /api/v1/agency/members | ❌ Route |
| 5 | DELETE /api/v1/provider/agency/members/{id} | DELETE /api/v1/agency/members/{id} | ❌ Route |
| 6 | GET /api/v1/provider/agency/applications | GET /api/v1/agency/applications | ❌ Route |
| 7 | POST /api/v1/provider/agency/applications/{id}/approve | POST /api/v1/agency/applications/{id}/approve | ❌ Route |
| 8 | POST /api/v1/provider/agency/applications/{id}/reject | POST /api/v1/agency/applications/{id}/reject | ❌ Route |

All 8 endpoints exist with **prefix `/api/v1/agency/`** instead of plan's `/api/v1/provider/agency/`**.

#### Guide ↔ Agency (5/5 exist — routes/methods differ)

| # | Plan Route | Actual Route | Status |
|---|-----------|-------------|--------|
| 1 | GET /api/v1/guides/agency-invitations | GET /api/v1/guides/me/invitations | ❌ Route |
| 2 | POST /api/v1/guides/agency-invitations/{id}/accept | POST /api/v1/guides/invitations/{id}/accept | ❌ Route |
| 3 | POST /api/v1/guides/agency-invitations/{id}/decline | POST /api/v1/guides/invitations/{id}/decline | ❌ Route |
| 4 | POST /api/v1/guides/apply-to-agency | POST /api/v1/guides/agency-applications | ❌ Route |
| 5 | POST /api/v1/guides/leave-agency | DELETE /api/v1/guides/me/agency | ❌ Route + Method (POST→DELETE) |

#### Agency Public (0/2 — MISSING ❌)

| # | Plan Route | Status |
|---|-----------|--------|
| 1 | GET /api/v1/agencies | ❌ NOT FOUND |
| 2 | GET /api/v1/agencies/{id} | ❌ NOT FOUND |

No public agency listing/detail endpoints exist in any endpoint file.

#### Provider Dashboard (3/4)

| # | Plan Route | Status |
|---|-----------|--------|
| 1 | GET /api/v1/provider/dashboard/overview | ✅ |
| 2 | GET /api/v1/provider/dashboard/notifications | ❌ MISSING |
| 3 | GET /api/v1/provider/dashboard/pending-actions | ✅ |
| 4 | GET /api/v1/provider/settings | ✅ |

---

## Integration Events

### Claimed: 8 events (4 new + 4 existing). Actual: 6 exist as described, 2 use generic contract.

| Event | Plan | Actual | Status |
|-------|------|--------|--------|
| ProviderApprovedIntegrationEvent | New | Exists in Accounts.Contracts | ✅ |
| ProviderSuspendedIntegrationEvent | New | Exists in Accounts.Contracts | ✅ |
| AgencyAffiliationCreatedIntegrationEvent | New | Exists in Accounts.Contracts | ✅ |
| AgencyAffiliationTerminatedIntegrationEvent | New | Exists in Accounts.Contracts | ✅ |
| ProviderApplicationSubmittedIntegrationEvent | Existing | ❌ Uses generic `ProviderStatusChangedIntegrationEvent` | ❌ |
| ProviderMoreDocsRequestedIntegrationEvent | Existing | ❌ Uses generic `ProviderStatusChangedIntegrationEvent` | ❌ |
| ProviderRejectedIntegrationEvent | Existing | Exists in Accounts.Contracts | ✅ |
| ProviderReinstatedIntegrationEvent | Existing | Exists in Accounts.Contracts | ✅ |

**Missing from plan**: `ProviderStatusChangedIntegrationEvent` — generic event published for ALL state transitions via `AccountsIntegrationConverters`. Also: `ProviderRegisteredDomainEvent` exists but not mentioned.

---

## Background Services

| Service | Exists | Complete | Status |
|---------|--------|----------|--------|
| AgencyInvitationExpiryService | ✅ | ✅ Fully functional | ✅ |
| ProviderDocumentExpiryService | ✅ | ⚠️ Has TODOs, incomplete notification | ⚠️ |

---

## Scorecard

| Dimension | Score | Notes |
|-----------|-------|-------|
| Design Decisions | 10/10 | All 9 correct |
| Entity Models | 9/10 | All entities exist, minor property naming drift |
| State Machine | 7/10 | Full machine except CRITICAL reapplication path |
| Post-Approval Automation | 10/10 | All 4 handlers perfect |
| Agency Roster | 9/10 | Fully implemented, route naming drift |
| Endpoint Accuracy | 6/10 | 3 missing, 13 route mismatches, 1 method mismatch |
| Integration Events | 7/10 | 6/8 accurate, 2 use generic, 1 omitted |
| Background Services | 8/10 | Both exist, 1 incomplete |
| Role Naming | 5/10 | "Tourist" used throughout, doesn't exist |
| **Overall** | **7.8/10** | Substantially implemented with gaps |

---

## All Discrepancies Summary

| # | Severity | Description |
|---|----------|-------------|
| 1 | 🔴 CRITICAL | Rejected → Draft reapplication path not implemented in ProviderApplication entity |
| 2 | ❌ HIGH | "Tourist" role referenced throughout plan — no such role; actual is Guest/User |
| 3 | ❌ MEDIUM | 2 integration events don't exist as separate contracts (use generic StatusChanged) |
| 4 | ❌ MEDIUM | 3 endpoints missing (2 public agency + 1 dashboard notifications) |
| 5 | ❌ MEDIUM | All 13 agency/guide endpoint routes use different prefixes than plan |
| 6 | ⚠️ LOW | Property name: `TerminatedBy` → actual `TerminatedByUserId` |
| 7 | ⚠️ LOW | Leave-agency endpoint: POST → actual DELETE method |
| 8 | ⚠️ LOW | ProviderDocumentExpiryService has TODOs (incomplete notification) |
| 9 | ⚠️ LOW | Plan omits ProviderStatusChangedIntegrationEvent (generic, exists for all transitions) |
| 10 | ⚠️ LOW | Plan omits ProviderRegisteredDomainEvent |
| 11 | ⚠️ LOW | AgencyInvitation has `IsExpired` property not in plan |
| 12 | ⚠️ LOW | CreatorApplicationApproved event: plan says `UserId`, actual is `ApplicantUserId` |
| 13 | ⚠️ LOW | EmailVerifiedIntegrationEvent: plan says "Auth.Contracts", actual is Security.Contracts |

---

## Key Architecture Facts

- **AccountsEndpoints.cs** mounts 5 route groups: `/api/v1/accounts`, `/api/v1/provider`, `/api/v1/admin/providers`, `/api/v1/agency`, `/api/v1/guides`
- **AccountsIntegrationConverters** publishes `ProviderStatusChangedIntegrationEvent` for ALL domain event transitions, PLUS dedicated events for Approved/Suspended/Rejected/Reinstated
- **IProviderStatusService**: Single method `IsApprovedProviderAsync()` — returns false for Suspended (checks approval, not explicitly non-suspension)
- **SecurityDataSeeder** seeds all 8 roles at runtime; **SecurityDbInitializer** is legacy with only 5 roles
- **RegisterExternalAsync()** assigns User directly, bypassing Guest → User email verification lifecycle
