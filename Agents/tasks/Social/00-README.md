# Social Module Sprint — Wave 6

> **Predecessor sprint:** Finance (Wave 5 — closed Thu 2026-10-15 after Booking)
> **This sprint covers:** Wave 6 Social slice — **Reviews** (verified-booking gate, weighted Bayesian rating, 5-report auto-hide), **Favorites** (toggle, max 500/user, FavoriteAddedIntegrationEvent for Discount notifications), **Reports** (5 reasons + auto-action), **Content Moderation Logs**, plus 1 BG service (`OrphanedFavoritesCleanupService` weekly Sat 03:00 UTC) and a shared `RatingRecalculationService` (daily 03:00 UTC, owned by Social → publishes integration events Analytics + ContentTours/ContentPlaces consume).
> **Difficulty vs Finance:** ⚙️⚙️ (2/5) — no money, no escrow, no gateway integration. The complexity is in the Bayesian rating math, the verified-booking eligibility chain (Booking inbox), and the profanity / NSFW pipeline.
> **Endpoint count:** **22 HTTP endpoints + 1 BG service + 1 RatingRecalculationService**
> **Working-day estimate:** **30 working days × 4 devs ≈ 130 person-hours** (6 weeks)

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Notes |
|---|---|---|
| Pre-work cut | Fri 2026-10-16 17:00 | All PW-1..PW-7 stubs (see `01-pre-work.md`) |
| Kickoff | Mon 2026-10-19 09:30 AST | Standup format same as Finance |
| PW merge deadline | Tue 2026-10-20 17:00 | Tech Lead applies migration `SocialAddAggregateRootAndAuditMembers` |
| Earliest task start | Wed 2026-10-21 09:00 | TASK 1 (Reviews) begins |
| Mid-sprint integration freeze | Sun 2026-11-15 17:00 | No new commits to public contracts |
| Hard PR cutoff | Wed 2026-11-25 17:00 | Merge cutoff |
| Sprint retro + demo | Fri 2026-11-27 11:00 AST | Folder moves to `Agents/decisions/closed/Social/` |

Working week Sun→Thu (5 days). Standup 09:30 AST 15 min hard cap.

---

## 1. Working Days & Person-Hour Budget

| Metric | Value |
|---|---|
| Working days | 30 |
| Hours/day/dev | 6 |
| Devs | 4 |
| Total hours | 720 |
| Task hours | 130 |
| Review hours | 20 |
| Ceremony hours | 15 |
| Buffer / unplanned | 555 (other projects + part-time on this sprint) |

This sprint runs in parallel with Messaging/Analytics if staffing allows — 4 devs are NOT fully dedicated to Social. Reviewer rotation: Mohammad reviews Mahmoud's TASK 1, Fadwa reviews Mohammad's TASK 4.

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG | Est hours | Hard deadline |
|---|---|---|---|---|---|---|
| **Mahmoud** | Intermediate | T1 Reviews CRUD + provider reply + edit window | 8 | 0 | 38h | Sun 2026-11-08 |
| **Mohammad** | Intermediate | T4 Reports + ContentModerationLog + admin moderation queue | 6 | 0 | 28h | Sun 2026-11-22 |
| **Fadwa** | Beginner | T2 Favorites + FavoriteAddedIntegrationEvent + T3 OrphanedFavoritesCleanup BG | 4 | 1 | 24h | Sun 2026-11-15 |
| **Mohammad** (lead) | Intermediate | T5 RatingRecalculationService + Bayesian formula + cross-module events | 0 | 1 | 24h | Sun 2026-11-22 |
| **Tech Lead** | Senior | PW + reviews + content moderation policy doc | – | – | 16h | – |

---

## 3. Endpoints Inventory (22)

### TASK 1 — Reviews (Mahmoud, 8 endpoints)
- `POST /api/v1/reviews` — Submit review (verified-booking eligibility check via Booking snapshot)
- `PUT /api/v1/reviews/{id}` — Edit own review within 48h window
- `DELETE /api/v1/reviews/{id}` — Soft-delete own review
- `POST /api/v1/reviews/{id}/reply` — Provider replies (must own target tour/place/business)
- `PUT /api/v1/reviews/{id}/reply/{replyId}` — Edit own reply (provider, unlimited window)
- `DELETE /api/v1/reviews/{id}/reply/{replyId}` — Soft-delete own reply
- `GET /api/v1/reviews/my-reviews` — Current user's reviews (cursor pagination, includes deleted=false)
- `GET /api/v1/reviews/admin/flagged` — Admin moderation queue (auto-hidden reviews)

### TASK 2 — Favorites (Fadwa, 4 endpoints)
- `POST /api/v1/favorites` — Add (entityType ∈ {Tour, Place, Business}, max 500 total)
- `DELETE /api/v1/favorites/{entityType}/{entityId}` — Remove
- `GET /api/v1/favorites` — List own favorites (cursor pagination, filter by entityType)
- `GET /api/v1/favorites/check/{entityType}/{entityId}` — Boolean check (cheap, used by frontend cards)

### TASK 4 — Reports + Moderation (Mohammad, 6 endpoints)
- `POST /api/v1/reports` — Submit report (5 reasons: Spam, Inappropriate, Misleading, Harassment, FakeReview, Other)
- `POST /api/v1/reviews/{id}/report` — Convenience alias for reports on reviews
- `POST /api/v1/reports/admin/{id}/resolve` — Admin resolves report (action: Dismiss, RemoveContent, WarnUser, BanUser)
- `POST /api/v1/reviews/admin/{id}/approve` — Restore a 5-report auto-hidden review
- `POST /api/v1/reviews/admin/{id}/remove` — Permanently remove a flagged review
- `GET /api/v1/reports/admin` — Admin reports list (filter by reason, status, entityType, dateRange)
- `GET /api/v1/moderation/logs` — Admin audit log of all moderation actions (cursor pagination)

### TASK 3 — BG: OrphanedFavoritesCleanupService (Fadwa, 0 endpoints)
- Weekly Sat 03:00 UTC. Removes Favorites pointing to soft-deleted entities (queries inbox snapshots from ContentPlaces/ContentTours).

### TASK 5 — BG: RatingRecalculationService (Mohammad, 0 endpoints)
- Daily 03:00 UTC. Recalculates `AverageRating` + `ReviewCount` for every Tour/Place/Business with non-trivial review counts. Emits `social.rating.recalculated.v1` integration events. ContentTours/ContentPlaces inbox handlers update their `AverageRating` / `ReviewCount` columns (denormalized read-path).

---

## 4. Integration Events Emitted (5)

| Logical name | Source | Used by |
|---|---|---|
| `social.review.published.v1` | Review.MarkPublished | Analytics (UserInteraction.ReviewSubmitted), Messaging (provider notification "New review on your tour") |
| `social.review.deleted.v1` | Review.Delete | Analytics (audit log), ContentTours/ContentPlaces (rating recalc trigger) |
| `social.favorite.added.v1` | Favorite.Add | Messaging (subscribe to discount notifications on this entity), Analytics (UserInteraction.AddToFavorite) |
| `social.report.resolved.v1` | Report.Resolve | Analytics (audit log), Messaging (notify reporter of outcome) |
| `social.rating.recalculated.v1` | RatingRecalculationService | ContentTours.InboxHandler (updates Tour.AverageRating + Tour.ReviewCount); ContentPlaces.InboxHandler (Place / Business equivalents) |

## 5. Integration Events Consumed (4)

| Logical name | Source module | Consumer |
|---|---|---|
| `booking.tour-booking.completed.v1` | Booking | `SocialBookingCompletedHandler` writes `social.BookingEligibilitySnapshot` row (verified-booking gate for `POST /reviews`) |
| `content-places.place.deleted.v1` | ContentPlaces | Triggers OrphanedFavoritesCleanupService backlog flag |
| `content-places.business.deleted.v1` | ContentPlaces | Same |
| `content-tours.tour.deleted.v1` | ContentTours | Same |

---

## 6. Out of Scope

- **AccessibilityReviews** (separate Phase 3 feature 14 from PDF 2 §14 — "Were features accurate?" Yes/No flow). Deferred to Phase 3 sprint.
- **Notification dispatching** — Social only emits integration events; Messaging sprint owns SignalR + email queue.
- **NSFW ML classification** — placeholder `INsfwClassifier` interface registered with `AlwaysSafeClassifier` stub. Real Azure Content Moderator integration deferred to Phase 4.
- **Profanity blocklist editor UI** — admin only edits via direct DB this sprint; UI in Phase 3.
- **Review photo upload** — deferred to Phase 3. This sprint accepts text-only reviews. The `ReviewPhotos` table + endpoint planned in T1's "future hooks" section, not built.

---

## 7. File Manifest (10 files)

| # | File | Purpose | Status |
|---|---|---|---|
| 1 | `00-README.md` | This file | this |
| 2 | `01-pre-work.md` | PW-1..PW-7 | next |
| 3 | `02-critical-rules.md` | S-R1..S-R10 | next |
| 4 | `03-entities-matrix.md` | Aggregates + events + migrations | next |
| 5 | `04-task-reviews.md` | T1 — Mahmoud | next |
| 6 | `05-task-favorites.md` | T2 — Fadwa | next |
| 7 | `06-task-reports-moderation.md` | T4 — Mohammad | next |
| 8 | `07-task-background-services.md` | T3 OrphanedFavorites + T5 RatingRecalc | next |
| 9 | `08-cross-cutting.md` | DI + permission + outbox parity + migrations + hygiene | next |
| 10 | `99-acceptance-gate.md` | Final sign-off | next |

When sprint closes, move whole folder to `Agents/decisions/closed/Social/`.
