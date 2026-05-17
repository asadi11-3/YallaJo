# Analytics — Wave 6 (Phase 2) Module Sprint

> **Predecessor sprint:** Messaging (`Agents/decisions/closed/Messaging/` once closed). Reuses the proven structure.
> **This sprint covers:** Phase 2 closure of the Analytics module — high-volume **UserInteractions** ingest, **PopularityScores** recalculation, **Popular / Trending** discovery endpoints, **admin & provider dashboards**, **audit log** API. Excludes Phase 4 recommendation engine (feature 21) and user preferences (feature 24).
> **Difficulty vs Messaging:** ⚙️⚙️⚙️ (3/5) — simpler than Messaging (no SignalR, no email reliability puzzle, no SLA round-robin). The hard parts are: (a) high-write-throughput ingest design without bottlenecking the BIGINT PK, (b) recency-window aggregation queries that stay sub-second on millions of rows, (c) the Bayesian-style trending DELTA calculation, (d) audit log redaction rules.
> **Endpoint count:** **17 HTTP endpoints + 1 BG service (PopularityScoreCalculationService 6h)**.
> **Working-day estimate:** **30 working days × 4 devs ≈ 130 person-hours**.

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Time (AST) | Owner |
|---|---|---|---|
| Pre-work cut (PR window opens) | Fri 2027-01-15 | 17:00 | Tech Lead |
| Sprint kickoff (standup #1) | Mon 2027-01-18 | 09:30 | All |
| Pre-work PRs merge deadline | Tue 2027-01-19 | 17:00 | Tech Lead |
| Earliest task PRs may open | Wed 2027-01-20 | 09:00 | All |
| Mid-sprint integration freeze | Sun 2027-02-14 | 17:00 | Tech Lead |
| **Hard PR cutoff** | Wed 2027-02-24 | 17:00 | All |
| **Hard merge-to-main cutoff** | Thu 2027-02-25 | 17:00 | Tech Lead |
| Sprint retro + demo | Fri 2027-02-26 | 11:00 | All |

Working week Sun→Thu (5 days). Daily standup 09:30 AST 15 min hard cap. Three sentences per dev (yesterday/today/blockers). Miss-2 rule applies.

---

## 1. Working Days & Person-Hour Budget

| Item | Hours |
|---|---|
| Working days | 30 |
| Hours/day/dev | 6 (focused) |
| Devs | 4 (3 builders + 1 Tech Lead pre-work + review) |
| **Total available** | **6×30×3 + 16 TL = 556** |
| Task hours (sum of T1..T6) | 110 |
| Review hours (TL) | 14 |
| Ceremony hours (standups + retro) | 6 |
| **Sprint floor** | **130** |
| Buffer | 426 |

Generous buffer reserved for: high-write ingest tuning (UserInteractions tends to surprise on first prod load), dashboard query plan optimization, audit log redaction false-positive triage, parallel sprints (Authorization-Cleanup may run concurrently with Analytics if Tech Lead has capacity).

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG services | Est. hours | Hard deadline |
|---|---|---|---|---|---|---|
| **Mahmoud** | Intermediate | T1 Interactions ingest + read endpoints | 3 | 0 | 24 | Sun 2027-01-31 |
| **Mohammad** | Intermediate (Lead) | T2 Popular/Trending + T3 PopularityScoreCalc BG | 4 | 1 | 36 | Sun 2027-02-14 |
| **Fadwa** | Beginner→Intermediate | T4 Admin Dashboards (kid gloves on aggregation queries) | 4 | 0 | 28 | Sun 2027-02-21 |
| **Mohammad / Mahmoud split** | — | T5 Provider Analytics dashboard | 3 | 0 | 14 | Sun 2027-02-21 |
| **Fadwa** | — | T6 Audit Logs query API + redaction | 3 | 0 | 14 | Sun 2027-02-21 |
| Tech Lead | — | PW-1..PW-7 + review + retro | — | — | 14 | continuous |
| **Sum** | | | **17** | **1** | **130** | |

**Pairing recommendation:** Fadwa pairs with Mohammad on first 2 days of T4 to set up the dashboard query helpers (window functions, GROUP BY ROLLUP, percentile calcs) — these are SQL she hasn't touched in YallaJo before. Once the SQL pattern is established, she runs solo.

---

## 3. Module Status After This Sprint

Per `agent-context.md` §11.1 — Analytics flips from ⬜ Empty to ✅ Phase 2 Complete.

**In scope (this sprint):**
- ✅ Interactions ingest (UserInteractions table, BIGINT PK) — fire-and-forget
- ✅ Popular tours/places (PopularityScores read API)
- ✅ Trending (DELTA last 7d vs previous 7d)
- ✅ Admin dashboards (revenue / bookings / users / time series)
- ✅ Provider analytics dashboard
- ✅ Audit log query API (AuditLogs table, retention 2 years, payment-data redacted)
- ✅ 1 BG: PopularityScoreCalculationService (6h)

**OUT OF SCOPE (deferred):**
- ❌ RecommendationCache + collaborative filtering (Phase 4 feature 21)
- ❌ UserPreferences + UserPreferredCategory CRUD (Phase 4 feature 24)
- ❌ A/B testing infrastructure (post-MVP)
- ❌ Real-time dashboard via SignalR (Phase 3) — v1 polls every 30s

---

## 4. Integration Events — Emitted & Consumed

### 4.1 Emitted by Analytics (3 outbox events)

| Logical name | Trigger | Payload |
|---|---|---|
| `analytics.popularity-scores.recalculated.v1` | PopularityScoreCalculationService finishes batch | `{recalculatedAt, entityCounts: {Tour, Place, Business}, durationMs}` |
| `analytics.audit-log.entry-redacted.v1` | Redaction job flags payment-data leak in old log | `{auditLogId, originalAction, redactedFields}` — admin alert |
| `analytics.trending.refreshed.v1` | After PopularityScoreCalc, trending deltas recomputed | `{refreshedAt, topMoversByEntityType}` |

### 4.2 Consumed by Analytics (~16 inbox handlers across modules)

| Source module | Logical name | Side effect |
|---|---|---|
| Booking | `booking.tour-booking.created.v1` | Insert UserInteraction(BookingStarted) + audit log row |
| Booking | `booking.tour-booking.confirmed.v1` | Insert UserInteraction(BookingCompleted) + audit log row |
| Booking | `booking.tour-booking.cancelled.v1` | Insert UserInteraction(BookingCancelled) + audit log row |
| Booking | `booking.tour-booking.completed.v1` | Update PopularityScores stale-flag for tour |
| Finance | `finance.payment.completed.v1` | Audit log row (redacted) |
| Finance | `finance.payout.completed.v1` | Audit log row + dashboard cache invalidate |
| Finance | `finance.refund.completed.v1` | Audit log row |
| Social | `social.review.published.v1` | Update PopularityScores stale-flag |
| Social | `social.favorite.added.v1` | Insert UserInteraction(AddToFavorite) |
| Social | `social.rating.recalculated.v1` | Bump PopularityScore.LastRatingUpdate stamp |
| ContentTours | `content-tours.tour.published.v1` | Initialize PopularityScores row + UserPreference snapshot |
| ContentTours | `content-tours.tour.deleted.v1` | Soft-delete PopularityScores row |
| ContentPlaces | `content-places.place.created.v1` | Initialize PopularityScores row |
| ContentPlaces | `content-places.place.deleted.v1` | Soft-delete PopularityScores row |
| Auth | `auth.user.registered.v1` | Audit log row (no PII in body, just userId) |
| Accounts | `accounts.provider.status-changed.v1` | Audit log row |

**~16 inbox handlers** to write. Most are 1-liner audit-log inserts. The 4 PopularityScores stale-flag handlers need a small `IPopularityScoreStaleFlagService` to dedupe (don't write 50 stale flags for the same tour in 1 minute — debounce 30s).

---

## 5. File map of this folder

| File | Purpose |
|---|---|
| `00-README.md` | This file |
| `01-pre-work.md` | PW-1..PW-7 (Tech Lead drives) |
| `02-critical-rules.md` | Analytics-specific rules A-R1..A-R10 |
| `03-entities-matrix.md` | Aggregates + new entities + 7 migrations |
| `04-task-interactions-ingest.md` | T1 Mahmoud — 3 endpoints + ~3 inbox handlers |
| `05-task-popular-trending.md` | T2 Mohammad — 4 endpoints + ~4 stale-flag inbox handlers |
| `06-task-admin-dashboards.md` | T4 Fadwa — 4 endpoints + ~3 inbox handlers (audit/dashboard cache invalidation) |
| `07-task-provider-analytics.md` | T5 split — 3 endpoints |
| `08-task-audit-log-query.md` | T6 Fadwa — 3 endpoints + ~6 audit-log inbox handlers + redaction |
| `09-task-background-services.md` | T3 Mohammad — PopularityScoreCalculationService 6h |
| `10-cross-cutting.md` | DI / perms / outbox parity / migrations / performance / data retention |
| `99-acceptance-gate.md` | Final sign-off checklist |

On close: `Move-Item -LiteralPath "Agents\tasks\Analytics" -Destination "Agents\decisions\closed\Analytics"`.
