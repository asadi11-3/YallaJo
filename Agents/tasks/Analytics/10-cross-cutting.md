# Analytics — Cross-Cutting Concerns

> Tech Lead enforces during PR review and at the sprint integration freeze (Sun 2027-02-14 17:00).

---

## 1. DI Audit

`Analytics.Infrastructure/DependencyInjection.cs`:

| Registration | Symbol | Lifetime | Reason |
|---|---|---|---|
| DbContext factory | `IDbContextFactory<AnalyticsDbContext>` | Singleton | BG service ingest drainer |
| Pooled DbContext | `AnalyticsDbContext` via `AddDbContextPool` | Scoped | Per-request handlers |
| Unit of Work | `IAnalyticsUnitOfWork → AnalyticsUnitOfWork` | Scoped | Domain event dispatch |
| Inbox store | `IAnalyticsInboxStore → AnalyticsInboxStore` | Scoped | Idempotency |
| Outbox writer | `IAnalyticsOutboxWriter → AnalyticsOutboxWriter` | Scoped | Integration event publishing |
| Repos | `IUserInteractionRepository`, `IPopularityScoreRepository`, `IAuditLogRepository`, `IDashboardCacheRepository`, `IEntityPopularitySnapshotRepository`, snapshot repos | Scoped | One per interface |
| `IInteractionIngestQueue` | `InteractionIngestQueue` | **Singleton** | Channel<T> shared across requests |
| `InteractionIngestDrainService` | BG service | Singleton | `AddHostedService<>` |
| `PopularityScoreCalculationService` | BG service | Singleton | `AddHostedService<>` |
| Permission catalog | `IPermissionCatalog → AnalyticsPermissionCatalog` | Singleton | Discovery |
| `IClientContextProvider` | `HttpContextClientContextProvider` | Singleton | PW-6 |
| `IAuditLogRedactor` | `AuditLogRedactor` | Singleton | Stateless redaction |
| `IAnalyticsCacheKeys` | `AnalyticsCacheKeys` | Singleton | Centralized key/tag building |
| MediatR | from `Analytics.Application` assembly | per-call | Handler discovery |
| FluentValidation | from `Analytics.Application` assembly, Scoped, internal | Scoped | Per-command validators |
| `AnalyticsDiagnostics` | static class | – | ActivitySource + Meter |

**Common mistakes:**
- ❌ Registering `IInteractionIngestQueue` as Scoped (Channel<T> singleton or it gets recreated → drain consumes wrong queue).
- ❌ Forgetting `AddHostedService<InteractionIngestDrainService>()` — POST /interactions silently drops everything.
- ❌ Adding `ICurrentUser` to handlers that don't appear in A-R9 audit list.

---

## 2. Permission seeder verification

Expected boot log:
```text
[INFO] PermissionSeeder discovered 11 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social, Messaging, Analytics
[INFO] PermissionSeeder inserted/verified 14 Analytics permissions
```

`SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Analytics.%'` → **14**.

---

## 3. Outbox / Inbox parity test

Add `tests/Analytics.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs` per prior sprint pattern.

**Logical names (3 emitted):**
```
analytics.popularity-scores.recalculated.v1
analytics.audit-log.entry-redacted.v1
analytics.trending.refreshed.v1
```

**Inbox consumers (~16 from `00-README.md §4.2`):**
```
auth.user.registered.v1
accounts.provider.status-changed.v1
booking.tour-booking.created.v1
booking.tour-booking.confirmed.v1
booking.tour-booking.cancelled.v1
booking.tour-booking.completed.v1
finance.payment.completed.v1
finance.payout.completed.v1
finance.refund.completed.v1
social.review.published.v1
social.favorite.added.v1
social.rating.recalculated.v1
content-tours.tour.published.v1
content-tours.tour.deleted.v1
content-places.place.created.v1
content-places.place.deleted.v1
```

---

## 4. Build lock workaround

```powershell
dotnet build Analytics/Analytics.Domain/Analytics.Domain.csproj
dotnet build Analytics/Analytics.Contracts/Analytics.Contracts.csproj
dotnet build Analytics/Analytics.Application/Analytics.Application.csproj
dotnet build Analytics/Analytics.Infrastructure/Analytics.Infrastructure.csproj
dotnet build Analytics/Analytics.Presentation/Analytics.Presentation.csproj
dotnet build tests/Analytics.Tests.Unit/Analytics.Tests.Unit.csproj
dotnet build tests/Analytics.IntegrationTests/Analytics.IntegrationTests.csproj
```

---

## 5. Migration sequence (7 migrations)

| # | Name | Owner | Task |
|---|---|---|---|
| 1 | `AnalyticsAddAggregateRootAndAuditMembers` | TL | PW-2 (only PopularityScore) |
| 2 | `AnalyticsAddUserInteractionIndexes` | Mahmoud | T1 |
| 3 | `AnalyticsAddPopularityScoreColumnsAndIndexes` | Mohammad | T2 |
| 4 | `AnalyticsAddEntityPopularitySnapshots` | Mohammad | T2 |
| 5 | `AnalyticsAddDashboardCache` | Fadwa | T4 |
| 6 | `AnalyticsAddAuditLogColumnsAndIndexes` | Fadwa | T6 |
| 7 | `AnalyticsAddIngestDebounceMarker` | Mohammad | T2 |

Plus T4 adds 3 snapshot tables in a sibling migration `AnalyticsAddDashboardSnapshotTables`.

---

## 6. Inbox / Outbox hygiene

- `CompositeOutboxProcessor` auto-picks up Analytics DbContext.
- OutboxCleaner deletes processed > 7 days. InboxCleaner > 30 days.
- **Alert:** `analytics.OutboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now - 5min` > 100 rows = page on-call.
- **Special alert:** UserInteractions table size — if growth rate > 50K rows/hour for 2 hours, alert SQL admin (potential bot attack or run-away client).

---

## 7. Performance Budget

| Endpoint | p95 target | Hard ceiling |
|---|---|---|
| POST /interactions | < 50 ms | < 100 ms |
| GET /popular/tours (cached) | < 80 ms | < 150 ms |
| GET /trending (cached) | < 80 ms | < 150 ms |
| GET /admin/dashboard (cached) | < 80 ms | < 200 ms |
| GET /admin/dashboard/revenue 1-month (cached) | < 100 ms | < 300 ms |
| GET /provider/dashboard (cached) | < 100 ms | < 200 ms |
| GET /admin/audit-logs cursor (uncached) | < 150 ms | < 400 ms |
| GET /admin/audit-logs/export 50K rows | < 8 s | < 15 s |

BG services:
- PopularityScoreCalculationService 10K entities < 60 s
- InteractionIngestDrainService 500 rows / sec sustained without backlog

---

## 8. Data Retention

Per A-R7:
- **UserInteractions:** no enforced retention this sprint. Likely 18-24 months acceptable; revisit Phase 3.
- **AuditLog:** 2-year retention per PDF. NOT enforced by code this sprint — manual SQL Agent job or future BG service.
- **EntityPopularitySnapshots:** keep 14 days (just enough for 7-day trending window). T3 BG service should `DELETE WHERE TakenAt < now-14d` at end of run.
- **DashboardCache:** TTL-based; rows with `ExpiresAt < now` swept by T3 BG service.
- **IngestDebounceMarker:** swept by T3 BG service > 1h.

**ADR Required (post-sprint):** "ADR-007 — Audit log retention enforcement strategy". TL drafts after retro.

---

## 9. Cross-Module Coupling Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Snapshot tables drift from authoritative (booking.TourBookings, finance.Payments, etc.) | Each inbox handler stamps `LastEventId` and `LastEventTimestamp` columns; daily reconciliation report shows entities with `LastEventTimestamp < now-1h` (stale) |
| UserInteractions FK to ContentTours fails after tour deletion | No FK — `EntityId` is loose reference; orphan rows tolerated (analytics over deleted entities OK) |
| RatingRecalculationService (Social) and PopularityScoreCalculationService (Analytics) both write to derived data | Mutual independence — Social writes EntityRatingCache, Analytics reads it through snapshot. No write conflict possible |
| Dashboard cache stale during heavy traffic | Cache TTL 30s is short enough; stampede protection via HybridCache lock |
| Audit log fills disk | Alert at 80% disk usage triggers SQL admin → run retention job manually until v2 BG service ships |

---

## 10. Secrets inventory

This sprint introduces NO new secrets. Reuses connection string + KeyVault setup from prior sprints.

---

## 11. Folder migration on close

After acceptance gate signed off:
```powershell
Move-Item -LiteralPath "Agents\tasks\Analytics" -Destination "Agents\decisions\closed\Analytics"
```
Update `Phase1-Phase2-Completion-INDEX.md` §1 row Analytics ⬜→✅ Phase 2.
Update `agent-context.md §11.1` Analytics row to ✅.
Add `AGENTS.md` entry for `Analytics/` module summary.
