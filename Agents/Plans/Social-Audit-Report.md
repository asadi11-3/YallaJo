# Social Module — Audit Report

> **Audited**: 2025-01-27
> **Plan**: `Agents/Plans/Social-Workflow.md` (530 lines)
> **Score**: **7.8/10**

---

## Executive Summary

The Social module is **well-implemented** with 13 entities, 23 endpoints, 22 CQRS handlers, 6 integration event converters, 2 background services, and 7 integration events in Contracts. Review moderation flow works with profanity check, auto-hide, and admin tools. EntityRatingCache with Bayesian scoring is solid. Key gaps: **permission catalog comment stale** (21 actual vs 19 claimed), **no public review listing endpoint** (only admin/my), and missing plan-specified features (Warn/Ban expansion, FavoriteEntityType expansion).

---

## Module Inventory

| Layer | .cs Files |
|---|---|
| Domain | 45 |
| Application | 65 |
| Infrastructure | 55 |
| Presentation | 5 |
| Contracts | 12 |
| **Total** | **182** |

---

## Domain Entities (13)

| Entity | Type | Notes |
|---|---|---|
| Review | Aggregate root | States: Published, AwaitingModeration, AutoHidden, RemovedByAdmin, DeletedByUser |
| ReviewReply | Child entity | Reply to review |
| ReviewHelpfulVote | Join entity | Composite key (ReviewId, UserId) |
| Report | Aggregate root | Flow: Open → UnderReview → Resolved/Dismissed |
| Favorite | Aggregate root | Soft-delete, idempotent |
| EntityRatingCache | Aggregate root | Bayesian scoring, recalculation events |
| ContentModerationLog | Entity | Admin audit trail |
| UserModerationRecord | Entity | Warning/ban tracking |
| ProfanityBlocklistEntry | Entity | Moderation lookup |
| TourSnapshot | Read model | Content snapshot for display |
| PlaceSnapshot | Read model | Content snapshot for display |
| BusinessSnapshot | Read model | Content snapshot for display |
| BookingEligibilitySnapshot | Read model | Eligibility check snapshot |

---

## Endpoints (23 REST)

| File | Routes | Notes |
|------|--------|-------|
| SocialEndpoints.cs | 0 | Wiring only |
| ReviewEndpoints.cs | 15 | CRUD + replies + helpful + admin |
| FavoriteEndpoints.cs | 4 | Add/remove/check/list |
| ReportEndpoints.cs | 3 | Submit/list/resolve |
| ModerationEndpoints.cs | 1 | Admin moderation log |
| **Total** | **23** | |

---

## Review Moderation Flow

```
Create → [profanity check]
  → Clean: Published
  → Dirty: AwaitingModeration → Admin Approve → Published
  
Published → [3 reports] → AutoHidden → Admin Restore → Published
Published → Admin Remove → RemovedByAdmin
Published → User Delete → DeletedByUser

Edit: only within 48 hours of creation
```

---

## EntityRatingCache

| Field | Type |
|---|---|
| TargetType | string |
| TargetId | Guid |
| AverageRating | decimal |
| ReviewCount | int |
| BayesianScore | decimal |
| LastRecalculatedAt | DateTime |

Emits: `EntityRatingRecalculatedDomainEvent`

---

## Integration Events (7 in Contracts)

1. ReviewAggregateUpdatedIntegrationEvent
2. ReviewPublishedIntegrationEvent
3. ReviewDeletedIntegrationEvent
4. RatingRecalculatedIntegrationEvent
5. ReportSubmittedIntegrationEvent
6. ReportResolvedIntegrationEvent
7. FavoriteAddedIntegrationEvent

---

## Integration Event Handlers (6 classes, 1 file)

All in `SocialIntegrationConverters.cs` — domain→outbox converters:
- ReviewPublished, ReviewDeleted, EntityRatingRecalculated, ReportSubmitted, ReportResolved, FavoriteAdded

**No inbound integration event consumers in Social.Infrastructure.**

---

## Background Services (2)

1. **RatingRecalculationService** — Periodic rating recalculation
2. **OrphanedFavoritesCleanupService** — Cleans favorites for deleted entities

---

## CQRS Handlers (22)

| Type | Count | Examples |
|---|---|---|
| Commands | 14 | CreateReview, EditReview, DeleteReview, RemoveReview, AddReviewReply, SubmitReport, ResolveReport, AddFavorite, RemoveFavorite, AddHelpfulVote |
| Queries | 8 | GetPublicReviews, GetMyReviews, GetRatingSummary, GetFlaggedReviews, GetAdminReports, CheckFavorite, GetMyFavorites, GetModerationLogs |

*Note: 8 additional application event handlers in `Social.Application/EventHandlers/` (not CQRS)*

---

## Permission Catalog

**Actual**: 21 permissions across 7 features  
**File comment claims**: 19 permissions across 6 features (STALE)

| Feature | Permissions |
|---|---|
| Review | Read, Create, Update, Delete |
| ReviewReply | Create, Update, Delete |
| Favorite | Read, Create, Delete |
| Report | Read, Create, Resolve |
| ContentModerationLog | Read |
| AdminModerationQueue | Read, Approve, Reject, Hide |
| UserModeration | Read, Warn, Ban |

---

## Key Gaps

| # | Gap | Severity | Notes |
|---|-----|----------|-------|
| 1 | Permission catalog comment stale (19→21) | LOW | Code is correct, comment wrong |
| 2 | No public review listing by entity | MEDIUM | Only admin/my queries exist |
| 3 | No HybridCache usage | MEDIUM | All queries hit DB directly |
| 4 | ReviewHelpfulVote — no query endpoint | LOW | Entity exists, no public route |
| 5 | Warn/Ban expansion (UserModeration) | MEDIUM | Entity exists, no full workflow |
| 6 | FavoriteEntityType expansion | LOW | Plan specifies more types |
| 7 | No inbound integration consumers | LOW | Social is event-source-only |

---

## Scorecard

| Dimension | Score | Notes |
|---|---|---|
| Plan Accuracy | 7/10 | Permission count stale; most structural claims correct |
| Implementation Completeness | 8/10 | Core features work; missing public reviews, cache |
| Code Quality | 8/10 | Proper Result pattern, state machines, event emission |
| Architecture Alignment | 9/10 | Clean separation, consistent patterns |
| **Overall** | **7.8/10** | |
