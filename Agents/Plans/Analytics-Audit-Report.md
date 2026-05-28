# Analytics Module — Audit Report

> **Audited**: 2025-01-27
> **Plan**: `Agents/Plans/Analytics-Workflow.md` (567 lines)
> **Score**: **6.8/10**

---

## Executive Summary

The Analytics module has strong content-based scoring and integration event handling but the plan document contains **13 factual inaccuracies** about the current state. The module has **51 endpoints** (not 47), **28 event handlers** (not 20), **8 background services** (not 10), and **9 anonymous routes** (not the 3 POST-only claim). Collaborative filtering and blended scoring remain entirely unimplemented. The 15 CQRS bypass endpoints in `RecommendationsEndpoints.cs` inject repositories directly — this is the plan's primary fix target and remains unfixed.

---

## Claims Verification

| # | Plan Claim | Actual | Status |
|---|-----------|--------|--------|
| 1 | 47 endpoints | **51** (Analytics 20 + Recommendations 29 + Preferences 2) | ❌ WRONG |
| 2 | All endpoints auth-decorated | **9 AllowAnonymous + 2 missing RequireAuthorization** | ❌ WRONG |
| 3 | 3 anonymous POSTs | **2 anonymous POSTs** (sponsored-click, metrics) + **7 anonymous GETs** | ❌ WRONG |
| 4 | 20 integration event handlers | **28** handlers | ❌ WRONG |
| 5 | 10 background services | **8** hosted services (2 .cs files are options classes) | ❌ WRONG |
| 6 | Collaborative filtering not built | **Confirmed** — 0 matches for ICollaborativeScoringEngine | ✅ CORRECT |
| 7 | Blended scoring not built | **Confirmed** — 0 matches for IBlendedScoringEngine | ✅ CORRECT |
| 8 | InteractionType 0-11 | **Confirmed** — View=0 through NotInterested=11 | ✅ CORRECT |
| 9 | Guide dashboard not built | **Partially wrong** — 3 guide endpoints exist (GET /guide/dashboard, /guide/analytics, /guide/my-tours) | ❌ WRONG |
| 10 | GDPR export not built | **Partially wrong** — exists at GET /me/export (not /gdpr/export as planned) | ❌ WRONG |
| 11 | 15 CQRS bypasses | **Confirmed** — all 15 in RecommendationsEndpoints.cs | ✅ CORRECT |
| 12 | Admin dashboard pre-computed | **Confirmed** — DashboardCache entity (Key, ValueJson, ExpiresAt, RebuiltAt) | ✅ CORRECT |
| 13 | Permission granularity gaps | **Confirmed** — 16 feature groups already exist in catalog | ✅ CORRECT |

---

## Endpoint Breakdown (51 total)

| File | Routes | Auth | Anonymous |
|------|--------|------|-----------|
| AnalyticsEndpoints.cs | 20 | 16 | 4 GETs |
| RecommendationsEndpoints.cs | 29 | 22 | 5 (2 POST + 3 GET) |
| PreferencesEndpoints.cs | 2 | 2 | 0 |
| **Total** | **51** | **40** | **9** |

**2 admin endpoints missing explicit RequireAuthorization** (have MustHavePermission metadata but no .RequireAuthorization() call).

---

## Background Services (8, not 10)

1. InteractionIngestDrainService
2. PopularityScoreCalculationService
3. SuggestionBatchRefreshJob
4. UserProfileUpdateJob
5. TripStageUpdateJob
6. GdprCleanupJob
7. EmailDigestBackgroundService
8. MetricsAggregationJob

---

## Integration Event Handlers (28, not 20)

| Source Module | Handlers |
|---|---|
| Booking | 4 (Created, Confirmed, Cancelled, Completed) |
| Finance | 5 (PaymentCompleted, PayoutCompleted, RefundCompleted, CommissionRuleUpserted, CommissionRuleDeleted) |
| Social | 4 (ReviewPublished, ReviewDeleted, FavoriteAdded, RatingRecalculated) |
| Content | 11 (Tours Created/Deleted/Updated, Places Created/Deleted/Updated, Business Created/Updated/Deleted, EntityCategory Assigned/Removed) |
| Auth | 1 (UserRegistered) |
| Booking Availability | 1 (SlotCapacityChanged) |
| Messaging | 2 (TicketCreated, TicketAssigned) |

---

## DashboardCache Entity Structure

| Property | Type | Notes |
|---|---|---|
| Key | string | Composite key |
| ValueJson | string | Pre-computed JSON |
| ExpiresAt | DateTime | TTL |
| RebuiltAt | DateTime | Last rebuild time |

**Note**: Plan's guide dashboard spec expects EntityType/EntityId/Granularity fields — these do NOT exist. DashboardCache is a simple key/value store.

---

## Permission Catalog (16 feature groups)

Interaction, PopularityScore, Trending, Recommendation, Preference, Batch, AdminDashboard, ProviderDashboard, AuditLog, BoostPackage, EditorialPin, SeasonalityRule, HolidayCalendar, Photogenic, Experiment, GuideDashboard

Permission groups: AnalyticsAccess, SystemAccess

---

## Scorecard

| Dimension | Score | Notes |
|---|---|---|
| Plan Accuracy | 5/10 | 8 of 13 factual claims wrong |
| Implementation Completeness | 7/10 | Core scoring + ingestion + events work; collaborative/blended missing |
| Code Quality | 6/10 | 15 CQRS bypasses, 9 auth gaps, 2 missing RequireAuthorization |
| Architecture Alignment | 8/10 | Pattern consistent with other modules |
| **Overall** | **6.8/10** | |

---

## Deferred / Follow-Up

1. Collaborative filtering engine (40% scoring weight) — major feature gap
2. Blended scoring assembly — depends on #1
3. Nightly matrix recomputation service — depends on #1
4. Fix 15 CQRS bypasses in RecommendationsEndpoints.cs
5. Fix auth on 9 anonymous + 2 admin endpoints
6. DashboardCache schema redesign for granularity support
7. Wire UserExcludedEntity into scorers
