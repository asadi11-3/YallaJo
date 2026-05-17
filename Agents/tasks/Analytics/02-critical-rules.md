# Analytics — Critical Rules (A-R1 .. A-R10)

> Additive to `Phase1-Phase2-Completion-INDEX.md` §4. Reviewers reject PRs that violate any rule.

---

## A-R1 — Fire-and-Forget Ingest

**The POST /interactions endpoint MUST return 202 within 50ms.** It cannot:
- Wait for `SaveChangesAsync` (does an async-enqueue into a Channel<T>)
- Call any aggregation logic
- Touch PopularityScores
- Resolve user from JWT (the endpoint accepts both authenticated and anonymous traffic — guest interactions stamped via cookie SessionId)
- Call rate limit middleware (it's already attached at pipeline level; the handler trusts that)

**Implementation:** `IInteractionIngestQueue` (Singleton, in-memory `Channel<InteractionEnvelope>(capacity: 10_000, BoundedChannelFullMode.DropWrite)`). A separate `InteractionIngestDrainService : BackgroundService` consumes the channel and writes via `IUserInteractionRepository.AddBatchAsync` in 100-row batches every 1 second (or sooner if channel >50).

**On channel full (DropWrite):** increment counter `analytics_ingest_dropped_total`. This is acceptable degradation under burst — interaction data is statistical, not transactional.

**Audit log is NOT fire-and-forget.** Audit log writes ride on the transaction of whatever business action caused them (see Booking/Finance inbox handlers in T6). Only USER INTERACTIONS are fire-and-forget.

---

## A-R2 — Interaction Deduplication

Per PDF 1 Wave 6 — "rate limit deduplicate same user+entity+type within 5 min".

**Logic:** before enqueue, check `IUserInteractionRepository.ExistsRecentDuplicateAsync(userId, entityType, entityId, interactionType, 5min)`. If true, drop silently (still return 202 — clients don't need to know).

**Dedup cache:** uses HybridCache key `interaction-dedupe:{userId}:{entityType}:{entityId}:{type}` with 5-min absolute expiration. Cache hit = skip DB check + drop. Cache miss = check DB, set cache, accept-or-drop.

**Guest users:** dedup keyed by `SessionId` not UserId. Cookie expires 30d.

---

## A-R3 — Popularity Score Formula

```
PopularityScore.Score = 
    (Views × 1.0) +
    (Clicks × 2.0) +
    (Favorites × 5.0) +
    (BookingsStarted × 8.0) +
    (BookingsCompleted × 15.0) +
    (Reviews × 4.0) +
    (RatingBonus = 0 if AvgRating < 3.5, else (AvgRating - 3.5) × 10) +
    (RecencyDecay penalty: each interaction's weight × 0.5 ^ (daysAgo / 30))
```

Coefficients are config: `Analytics:Popularity:Coefficients` section. **Admin can tune without redeploy** (cache TTL 1 hour for coefficients).

Recency decay = half-life 30 days. Implemented in T3 BG service via window-function SQL not LINQ (perf).

**Score column:** `decimal(18,4)`. Range observed in practice 0.0 to ~10_000.0.

---

## A-R4 — Trending DELTA Calculation

Trending ≠ Popular (PDF 1 Wave 6). Trending = "highest score delta last 7 days vs previous 7 days, captures viral/seasonal spikes".

**Algorithm (in T3 BG service):**
1. Read current PopularityScores for entity type (top 200 by absolute score for performance).
2. Read EntityPopularitySnapshots taken 7 days ago for same entity IDs.
3. For each entity: `delta = currentScore - snapshotScore_7d_ago`. If snapshot missing (entity created <7d ago), `delta = currentScore × 0.5` (penalty for new-but-no-baseline).
4. Sort descending by delta. Top 50 stored back into PopularityScore.TrendingRank (1..50, null for rest).
5. After computing, snapshot today's scores into EntityPopularitySnapshots(takenAt=today) for tomorrow's run.

**Why score-delta not interaction-delta:** interaction counts double-count weighted events; score-delta already weights them per A-R3.

---

## A-R5 — Dashboard Cache Strategy

Admin dashboards aggregate over time windows that don't change retroactively (yesterday's revenue is fixed). Cache aggressively.

| Endpoint | Cache key | TTL | Invalidation trigger |
|---|---|---|---|
| `GET /admin/dashboard` | `admin:dashboard:overview` | 30s sliding | finance.payment.completed, booking.tour-booking.created |
| `GET /admin/dashboard/revenue?period=...` | `admin:dashboard:revenue:{periodHash}` | 30s sliding | finance.payment.completed, finance.refund.completed |
| `GET /admin/dashboard/bookings?period=...` | `admin:dashboard:bookings:{periodHash}` | 30s sliding | booking.tour-booking.{created,cancelled,confirmed} |
| `GET /popular/{tours,places}` | `popular:{entityType}` | 5min | analytics.popularity-scores.recalculated.v1 (every 6h) |
| `GET /trending` | `trending:{entityType}` | 5min | analytics.trending.refreshed.v1 (every 6h) |
| `GET /provider/analytics` | `provider:dashboard:{providerId}` | 60s sliding | finance.payout.completed.v1, booking.tour-booking.completed.v1 |
| `GET /audit-logs` (admin) | NEVER cache | — | always live |

**DashboardCache table:** for endpoints with TTL >30s that involve heavy aggregation (popular, trending), persist the JSON-serialized result in `analytics.DashboardCache(Key PK, ValueJson, ExpiresAt, RebuiltAt)`. HybridCache wraps it (L1 in-memory + DashboardCache as L2 across instances).

**Why `analytics.DashboardCache` instead of full Redis:** ADR-003 deferred Redis to Phase 3. SQL Server caching table is "good enough" for v1 traffic.

---

## A-R6 — Audit Log Redaction Rules

Per PDF 1 Wave 6 — "payment data redacted".

**Hard redact (never store) in `AuditLog.OldValue` / `NewValue`:**
- `cardNumber` / `cvv` / `expiryDate` / `cardholderName` (PCI)
- `password` / `passwordHash` / `secret` / `apiKey`
- `webhookSignature` / `accessToken` / `refreshToken`

**Soft redact (store hash only) — for forensics correlation without exposure:**
- `email` (store `SHA256(email)[:16]` prefix)
- `phone` (store last-4 only)
- `ipAddress` (store /24 prefix only)

**Implementation:** `IAuditLogRedactor` (Application interface, Infrastructure impl using string scanners + JSON traversal). Called by every inbox handler before `AppendAsync`. Reusable across modules — lives in `SharedKernel.Application/Abstractions/Audit/` so Finance/Booking can use the same redactor in their own future audit writes.

**Retroactive redaction:** T6 includes a `POST /audit-logs/{id}/redact` admin endpoint that re-runs the redactor + flags `RedactedAt = now`. Emits `analytics.audit-log.entry-redacted.v1` to alert other admins (transparency).

---

## A-R7 — Audit Log Retention

**Retention: 2 years** per PDF 1 Wave 6.

**THIS SPRINT:** schema + query API. Retention enforcement DEFERRED — runs once via SQL Agent or manual cron until v2 BG service. Add a `Decision Required` annotation in `10-cross-cutting.md` so ops knows.

**Reason for deferring:** clean retention is straightforward bulk `DELETE TOP @batch WHERE OccurredAt < @threshold`. Doesn't need full BG-service framing v1.

---

## A-R8 — Cursor Pagination

Same shape as Booking/Finance/Social/Messaging (INDEX §4 R9). Differences:
- UserInteraction + AuditLog use **BIGINT IDENTITY PK**, so cursor encodes `(Id BIGINT, OccurredAt DateTime)` not `(Guid, DateTime)`.
- Special `BigIntCursor` record in `Analytics.Application/Pagination/`.
- PageSize clamped [1,50], default 20 — same as other modules.
- AuditLog default sort: OccurredAt DESC (newest first).
- PopularityScores default sort: Score DESC.

---

## A-R9 — ICurrentUser Audit

`ICurrentUser` is allowed ONLY in these handlers:

| Handler | Use |
|---|---|
| `GetProviderAnalyticsQueryHandler` | resolve provider via `BookingProviderSnapshot.UserId == ICurrentUser.UserId` (self-only) |
| `RecordInteractionCommandHandler` | stamp UserId or fallback to anonymous SessionId |
| `GetMyAuditLogQueryHandler` (deferred Phase 3 — not in this sprint, just reserved) | self-only |

**All other handlers MUST use `MustHavePermission`** — no ICurrentUser leak into Application layer.

---

## A-R10 — Error Codes

| Code | Outcome | When |
|---|---|---|
| `Interaction.InvalidEntityType` | 422 | EntityType enum value rejected |
| `Interaction.EntityNotFound` | 404 | entityId not in current snapshot tables |
| `Interaction.RateLimited` | 429 | hit per-user/min cap (handled by middleware, but error code reserved for sub-second client-side throttling) |
| `PopularityScore.NotFound` | 404 | entity has no scores row (means popularity hasn't been recalc'd since entity created — return empty score 0.0 instead of 404 in practice) |
| `Trending.WindowNotReady` | 503 | sprint just deployed, no 7-day-old snapshots — return empty list with status code |
| `AdminDashboard.PeriodInvalid` | 422 | startDate > endDate or > 1 year span |
| `AdminDashboard.GranularityNotSupported` | 422 | granularity not in {Day, Week, Month, Year} |
| `ProviderDashboard.NotProvider` | 403 | ICurrentUser is not registered as a provider |
| `ProviderDashboard.ProviderSnapshotMissing` | 503 | snapshot inbox hasn't caught up; retry in 30s |
| `AuditLog.NotFound` | 404 | log entry doesn't exist |
| `AuditLog.AlreadyRedacted` | 409 | redact called on a row where RedactedAt is non-null |
| `AuditLog.RedactionNotApplicable` | 422 | no sensitive fields detected — admin should not be redacting blindly |
| `AuditLog.ExportTooLarge` | 422 | export result > 100K rows; force date range narrowing |
