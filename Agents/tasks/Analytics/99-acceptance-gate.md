# Analytics Module — Final Acceptance Gate

> Tech Lead signs off before declaring Analytics sprint closed (Thu 2027-02-25 17:00).
> Folder MUST NOT move to `Agents/decisions/closed/Analytics/` until every box ticked.

---

## 1. Code Quality

- [ ] All 6 task PRs (T1..T6) merged into `main`.
- [ ] `dotnet build` green per `10-cross-cutting.md §4`.
- [ ] `<TreatWarningsAsErrors>` regressions = 0.
- [ ] `rg "TODO|FIXME|HACK" Analytics/` → 0 matches (any remaining → GitHub issue).
- [ ] All new FluentValidation rules have passing tests.
- [ ] All command handlers inject `ILogger<THandler>` + `RemoveByTagAsync` after SaveChanges (sample 3).
- [ ] All queries implement `ICacheableQuery` where applicable (sample 3).
- [ ] `ICurrentUser` only in handlers listed in `02-critical-rules.md §A-R9`.
- [ ] No bare `RequireAuthorization()`: `rg "RequireAuthorization\(\)\s*$" Analytics/Analytics.Presentation/` → 0 matches.
- [ ] `Analytics.Tests.Unit` ≥ **50 tests** passing.
- [ ] `Analytics.IntegrationTests` ≥ **15 tests** passing.
- [ ] `IntegrationEventTypeRegistryParityTests` passes.

---

## 2. Endpoint Smoke Test (17 endpoints)

Reviewer (Mohammad) records results in `Analytics/_smoke-test-runbook.md` (deleted before folder moves).

| # | Method | Path | Expected | Task |
|---|---|---|---|---|
| 1 | POST | `/api/v1/interactions` | 202 within 50ms | T1 |
| 2 | POST | `/api/v1/interactions` (dupe within 5min) | 202 + no DB write | T1 |
| 3 | GET | `/api/v1/admin/interactions` | 200 + cursor | T1 |
| 4 | GET | `/api/v1/admin/interactions/user/{userId}` | 200 | T1 |
| 5 | GET | `/api/v1/popular/tours` | 200 + top 20 sorted | T2 |
| 6 | GET | `/api/v1/popular/places` | 200 | T2 |
| 7 | GET | `/api/v1/popular/businesses` | 200 | T2 |
| 8 | GET | `/api/v1/trending` (fresh deploy) | 503 WindowNotReady | T2 |
| 9 | GET | `/api/v1/trending` (after 14d data) | 200 + TrendingRank populated | T2 |
| 10 | GET | `/api/v1/admin/dashboard` | 200 + 4 sections | T4 |
| 11 | GET | `/api/v1/admin/dashboard/revenue?from=X&to=Y` | 200 + time series | T4 |
| 12 | GET | `/api/v1/admin/dashboard/bookings?...` | 200 + funnel | T4 |
| 13 | GET | `/api/v1/admin/dashboard/users?...` | 200 + breakdown | T4 |
| 14 | GET | `/api/v1/provider/dashboard` (as provider) | 200 + self data | T5 |
| 15 | GET | `/api/v1/provider/analytics` | 200 | T5 |
| 16 | GET | `/api/v1/provider/my-tours` | 200 + cursor | T5 |
| 17 | GET | `/api/v1/admin/audit-logs` | 200 + cursor | T6 |
| 18 | POST | `/api/v1/admin/audit-logs/{id}/redact` | 200 + RedactedAt set | T6 |
| 19 | GET | `/api/v1/admin/audit-logs/export?from=X&to=Y` | 200 + CSV stream | T6 |

Any RED row blocks sign-off.

---

## 3. Outbox / Inbox Round-Trip

| Action | Outbox row | Logical name | Downstream side-effect | SLA |
|---|---|---|---|---|
| PopularityScoreCalculationService tick (T3) | 1 row | `analytics.popularity-scores.recalculated.v1` | popular/trending caches evicted; next GET returns fresh data | < 30s after tick |
| Trending recompute | 1 row | `analytics.trending.refreshed.v1` | (no external consumer this sprint, log only) | < 30s |
| POST /admin/audit-logs/{id}/redact (T6) | 1 row | `analytics.audit-log.entry-redacted.v1` | other admins see notification (Messaging consumer) | < 30s |

Inbox examples:
| Inbox event | Side effect | SLA |
|---|---|---|
| `booking.tour-booking.created.v1` | UserInteraction(BookingStarted) row + dashboard cache invalidate + AuditLog row | < 30s |
| `finance.payment.completed.v1` | PaymentSnapshot upserted + AuditLog row + revenue cache evict | < 30s |
| `content-tours.tour.published.v1` | PopularityScore row initialized (Score=0) | < 30s |

---

## 4. 24h Background Service Soak

- [ ] `PopularityScoreCalculationService` ticked **4 times** in 24h (every 6h).
- [ ] `InteractionIngestDrainService` ticked continuously, drained 100% of channel.
- [ ] OTEL: `popularity_recalc_failures_total` = 0.
- [ ] OTEL: `analytics_ingest_dropped_total` = 0 (or known acceptable count from load test).
- [ ] Zero `[ERROR]` Serilog entries from BG services.
- [ ] `EntityPopularitySnapshots` table has 4 daily snapshots (or skipped if < 14d uptime).
- [ ] `IngestDebounceMarker` rows older than 1h cleaned at end of each tick.

---

## 5. Performance Sanity (p95)

k6 / JMeter 5 minutes, 50 concurrent users:

| Endpoint | p95 target | Actual |
|---|---|---|
| POST /interactions | < 50 ms | _____ |
| GET /popular/tours (cached) | < 80 ms | _____ |
| GET /trending (cached) | < 80 ms | _____ |
| GET /admin/dashboard (cached) | < 80 ms | _____ |
| GET /admin/dashboard/revenue (cached) | < 100 ms | _____ |
| GET /provider/dashboard (cached) | < 100 ms | _____ |
| GET /admin/audit-logs cursor | < 150 ms | _____ |
| POST /interactions sustained 1000/sec | 0 drops | _____ |
| PopularityScoreCalc full sweep 10K entities | < 60 s | _____ |

---

## 6. Documentation Hygiene

- [ ] All new endpoints have XML doc summaries (auto-flows to Swagger).
- [ ] All 14 Analytics permissions listed in `Agents/permissions-inventory.md`.
- [ ] **Folder moves to `Agents/decisions/closed/Analytics/`**:
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Analytics" -Destination "Agents\decisions\closed\Analytics"
  ```
- [ ] `Phase1-Phase2-Completion-INDEX.md` §1 row Analytics ⬜→✅, link updated to closed/.
- [ ] `AGENTS.md` entry for Analytics/ module created.
- [ ] `agent-context.md §11.1` Analytics row → ✅ Phase 2.
- [ ] `Agents/error-log.md` updated with any new gotchas (high-write index tuning, dashboard cache stampedes, etc.).
- [ ] **ADR-007 drafted** if retention enforcement decision was made during sprint.

---

## 7. Sprint Retro & Demo (Fri 2027-02-26 11:00 AST)

15-min demo by Mohammad walking through:
1. Live POST /interactions burst (500/sec) → channel drains, dashboard reflects within 30s.
2. PopularityScoreCalc tick + trending DELTA computation.
3. Admin dashboard with live data update via cache invalidation.
4. Provider dashboard showing self-scoped data.
5. Audit log query + redaction live demo.

Retro doc `Agents/decisions/closed/Analytics/_retro.md`:
- What went well
- What hurt
- Action items for NEXT sprint (Authorization-Cleanup likely runs in parallel or follows)

---

## 8. Sign-Off

| Role | Name | Date | Signature |
|---|---|---|---|
| T1 owner | Mahmoud | _____ | _____ |
| T2 owner | Mohammad | _____ | _____ |
| T3 owner | Mohammad | _____ | _____ |
| T4 owner | Fadwa | _____ | _____ |
| T5 owner | Mohammad/Mahmoud | _____ | _____ |
| T6 owner | Fadwa | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |

Once signatures collected → folder moves → INDEX updated → module marked ✅ in agent-context.md §11.1 → Authorization-Cleanup sprint may already be running in parallel.
