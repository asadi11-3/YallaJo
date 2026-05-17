# Social Module — Final Acceptance Gate

> Tech Lead signs off before declaring the Social sprint closed (Sun 2026-11-22 17:00). Folder moves to `Agents/decisions/closed/Social/` only after every box is ticked.

---

## 1. Code Quality

- [ ] All 5 task PRs (T1, T2, T3, T4, T5) merged into `main`.
- [ ] `dotnet build` green for every Social project + 2 test projects (`08-cross-cutting.md §4`).
- [ ] `rg "TODO|FIXME|HACK" Social/` → 0 results (or all migrated to GitHub issues).
- [ ] **All command handlers** inject `ILogger<THandler>` + call `RemoveByTagAsync` after SaveChanges (sample 3).
- [ ] **All queries** implement `ICacheableQuery` (sample 3).
- [ ] **ICurrentUser audit** (S-R8) — only 12 documented handlers inject it; all others compile without it.
- [ ] **No bare `RequireAuthorization()`** in any new Presentation endpoint.
- [ ] `Social.Tests.Unit` green — minimum **45 tests** (12 reviews + 8 favorites + 10 reports + 4 cleanup + 8 rating + 3 misc).
- [ ] `Social.IntegrationTests` green — minimum **10 tests** (5 outbox round-trip + 3 cross-module inbox + 2 BG live-test).
- [ ] **Permission catalog parity test passes**.

---

## 2. Endpoint Smoke Test (manual Postman)

A reviewer runs this checklist live, records results in `Social/_smoke-test-runbook.md` (deleted before folder move).

| # | Method | Path | Expected | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/reviews` (verified booking) | 201 + IsVerifiedBooking=true | T1 |
| 2 | POST | `/api/v1/reviews` (no booking) | 201 + IsVerifiedBooking=false | T1 |
| 3 | POST | `/api/v1/reviews` (profanity) | 201 + Status=AwaitingModeration | T1 |
| 4 | POST | duplicate review same target | 409 `Review.AlreadyExists` | T1 |
| 5 | PUT | review at 47h | 200 | T1 |
| 6 | PUT | review at 49h | 422 `Review.EditWindowExpired` | T1 |
| 7 | DELETE | own review | 204 | T1 |
| 8 | POST | reply by tour-owning provider | 201 | T1 |
| 9 | POST | reply by non-owning provider | 403 | T1 |
| 10 | GET | `/api/v1/reviews/my-reviews` | 200 + cursor envelope | T1 |
| 11 | GET | `/api/v1/reviews/admin/flagged` | 200 + only AutoHidden + AwaitingModeration | T1 |
| 12 | POST | `/api/v1/favorites` (tour exists) | 201 | T2 |
| 13 | POST | favorite for deleted tour | 400 `Favorite.InvalidEntityType` | T2 |
| 14 | POST | duplicate favorite | 409 | T2 |
| 15 | POST | 501st favorite | 422 `Favorite.LimitExceeded` | T2 |
| 16 | DELETE | favorite that exists | 204 | T2 |
| 17 | DELETE | favorite that doesn't exist | 204 (idempotent) | T2 |
| 18 | GET | `/api/v1/favorites` | 200 + cursor + filter | T2 |
| 19 | GET | `/api/v1/favorites/check/Tour/{id}` | 200 + `{isFavorited:bool}` | T2 |
| 20 | POST | `/api/v1/reports` (review, valid reason) | 201 | T4 |
| 21 | POST | 5th report on same review | 201 + review auto-hidden (verify via `GET admin/flagged`) | T4 |
| 22 | POST | duplicate report by same user | 409 `Report.AlreadyReported` | T4 |
| 23 | POST | resolve report Dismiss | 200 | T4 |
| 24 | POST | resolve report RemoveContent | 200 + review deleted | T4 |
| 25 | POST | approve auto-hidden review | 200 + review restored | T4 |
| 26 | POST | remove flagged review | 200 + permanent delete | T4 |
| 27 | GET | `/api/v1/reports/admin` | 200 + cursor + filters | T4 |
| 28 | GET | `/api/v1/moderation/logs` | 200 + cursor + filters | T4 |

Any RED row blocks sign-off.

---

## 3. Outbox / Inbox Round-Trip

| Action | Outbox row | Logical name | Downstream side-effect | SLA |
|---|---|---|---|---|
| Publish review (T1) | 1 row | `social.review.published.v1` | Messaging notifies tour owner; Analytics writes UserInteraction.ReviewSubmitted | 30s |
| Delete review (T1) | 1 row | `social.review.deleted.v1` | ContentTours / ContentPlaces inbox trigger rating recalc; Analytics audit row | 30s |
| Add favorite (T2) | 1 row | `social.favorite.added.v1` | Messaging subscribes user to discount notifications on entity; Analytics writes UserInteraction.AddToFavorite | 30s |
| Resolve report (T4) | 1 row | `social.report.resolved.v1` | Messaging notifies reporter of outcome | 30s |
| Daily rating recalc (T5) | N rows | `social.rating.recalculated.v1` per entity | ContentTours.Tour.AverageRating updated; ContentPlaces.Place / Business equivalents | 60s |

**Inbox round-trip:**
| Action | Inbox row | Side-effect |
|---|---|---|
| Booking publishes `tour-booking.completed.v1` | row in `social.InboxMessages` | `BookingEligibilitySnapshot` upserted within 30s |
| ContentPlaces publishes `place.deleted.v1` | row in inbox | `PlaceSnapshot.IsDeleted = true` within 30s |
| ContentTours publishes `tour.deleted.v1` | row in inbox | `TourSnapshot.IsDeleted = true` within 30s |

---

## 4. Background Services Live Test (24h soak)

- [ ] `SELECT COUNT(*) FROM social.Favorites f LEFT JOIN social.TourSnapshots t ON t.TourId = f.EntityId WHERE f.EntityType = 0 AND f.IsDeleted = 0 AND (t.TourId IS NULL OR t.IsDeleted = 1)` → 0 (after one Saturday tick).
- [ ] Daily 03:00 UTC: `SELECT COUNT(*) FROM social.EntityRatingCache WHERE LastRecalculatedAt < SYSUTCDATETIME() - INTERVAL '25 hours'` → 0 (all entities with reviews recalculated within last 24h).
- [ ] OTEL: `bg_service_failures_total{service=OrphanedFavoritesCleanupService}` = 0.
- [ ] OTEL: `bg_service_ticks_total{service=RatingRecalculationService}` = 1 per 24h.
- [ ] No `[ERROR]`-level Serilog entries from either service in 24h.

---

## 5. Performance Sanity (p95)

| Endpoint | p95 target | Hard ceiling |
|---|---|---|
| POST `/reviews` | < 300 ms | < 600 ms |
| PUT `/reviews/{id}` | < 200 ms | < 500 ms |
| GET `/reviews/my-reviews` | < 100 ms (cached) | < 300 ms |
| GET `/reviews/admin/flagged` | < 150 ms (cached) | < 400 ms |
| POST `/favorites` | < 200 ms | < 500 ms |
| GET `/favorites` | < 80 ms (cached) | < 250 ms |
| GET `/favorites/check/{type}/{id}` | < 40 ms (cached) | < 150 ms |
| POST `/reports` | < 250 ms | < 600 ms |
| POST `/reports/admin/{id}/resolve` | < 300 ms | < 700 ms |
| RatingRecalculationService full sweep (10K entities) | < 60s | < 5 min |
| OrphanedFavoritesCleanup full sweep | < 30s | < 2 min |

SQL profiler: no query > 100 ms in the test (excluding the rating recalc batch).

---

## 6. Documentation Hygiene

- [ ] Every new endpoint has XML doc summary + `<param>` + `<returns>`.
- [ ] All 19 Social permissions listed in `Agents/permissions-inventory.md`.
- [ ] **Sprint folder moves to `Agents/decisions/closed/Social/`:**
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Social" -Destination "Agents\decisions\closed\Social"
  ```
- [ ] Master `Phase1-Phase2-Completion-INDEX.md` §1 row for Social marked 🟢 + linked to closed/ path.
- [ ] `AGENTS.md` gets a Social/ line entry.
- [ ] `agent-context.md §11.1` Social row updated to ✅ Phase 1 (PDF 1 Wave 6 first half).
- [ ] `Agents/error-log.md` gets new entries for any gotchas hit (especially Bayesian recency edge cases, snapshot-staleness races).
- [ ] ADR-007 considered: "Why we keep EntityRatingCache as IAggregateRoot (vs transient batch aggregate)" — file decision in `Agents/decisions/`.

---

## 7. Sprint Retro & Demo (Fri 2026-11-27 11:00 AST)

15-min demo by Mahmoud:
1. Live POST review with verified booking → outbox row → Messaging notification.
2. POST 5 reports rapid-fire → review auto-hides → admin queue shows it → admin approve restores.
3. RatingRecalculationService manual trigger (if T7 bonus endpoint added) → show new AverageRating + BayesianScore.
4. OrphanedFavoritesCleanupService dry-run query showing orphan count, then real run.

Retro doc at `Agents/decisions/closed/Social/_retro.md`:
- What went well (≥ 3 items)
- What hurt (≥ 3 items + linked error-log.md entries)
- Action items for next sprint (Messaging — likely scheduled Mon 2026-11-30 kickoff)

---

## 8. Sign-Off Block

| Role | Name | Date | Signature |
|---|---|---|---|
| T1 Reviews owner | Mahmoud | _____ | _____ |
| T2 Favorites owner | Fadwa | _____ | _____ |
| T3 OrphanedFavoritesCleanup owner | Fadwa | _____ | _____ |
| T4 Reports + Moderation owner | Mohammad | _____ | _____ |
| T5 RatingRecalculation owner | Mohammad | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |

Once collected, folder moves, INDEX updates, module marked ✅ Phase 1 in `agent-context.md §11.1`, Messaging sprint kickoff scheduled.
