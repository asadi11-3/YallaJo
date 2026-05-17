# Analytics — Pre-Work Kickoff Briefing

> Sprint window: 2027-01-18 → 02-26 (Phase 2 closure). Owner: Mahmoud.

## What's already wired

- `IAnalyticsUnitOfWork` + `AnalyticsUnitOfWork` delegate.
- 5 aggregate roots:
  - `PopularityScore` (Guid key)
  - `UserInteraction` (**BIGINT key** — `BaseEntity<long>, IAggregateRoot`)
  - `AuditLog` (**BIGINT key** — `BaseEntity<long>, IAggregateRoot`)
  - `RecommendationCache` (Guid key)
  - `UserPreference` (Guid key)
  - `UserPreferredCategory` remains a POCO/owned child (not aggregate).
- 10 domain events in `Analytics.Domain/Events/` covering popularity refresh, recommendations, audit log creation, interactions, preferences, bulk exports, redactions.
- 3 integration events registered as `analytics.popularity.refreshed.v1`, `analytics.recommendation-cache.expired.v1`, `analytics.audit.exported.v1`.
- 5 repositories — repository interfaces use the correct key type per entity: `IUserInteractionRepository : IRepository<UserInteraction, long>`, `IAuditLogRepository : IRepository<AuditLog, long>`, others on Guid. Plus `IAnalyticsOutboxWriter`.
- `IClientContextProvider` + `ClientContext(IpAddress?, UserAgent?, SessionId?, DeviceType?)` record + `NoopClientContextProvider` stub.
- `AnalyticsFeatures` (6) + `AnalyticsPermissionCatalog` (14 perms — AnalyticsAccess + SystemAccess groups for audit/admin).
- Test projects scaffolded.

## Day-0 sprint tasks (PW-9 lands here)

1. **PW-9 schema decisions** — EF migration for:
   - Clustered index on `UserInteraction(OccurredAt DESC, Id ASC)` (drop default PK clustered index, add explicit non-clustered PK).
   - Same on `AuditLog(OccurredAt DESC, Id ASC)`.
   - Document the `BigIntCursor` type in `Analytics.Application` for keyset pagination using the new clustered index.
2. Replace `NoopClientContextProvider` with real implementation reading from `IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress` / `Request.Headers.UserAgent`.
3. Wire `UserInteraction` fire-and-forget capture pipeline — a `Channel<UserInteraction>` background reader to absorb interaction recording with minimal hot-path cost.
4. Implement Bayesian-weighted `PopularityScore` refresh in `TrendingRefreshHostedService`.
5. Wire 7-day trending delta query — uses `OccurredAt` index from step 1.

## Watchpoints

- BIGINT keys: `IAggregateRoot` is base-class agnostic — the marker is on `BaseEntity<long>` for these two entities. Repositories type-parameterise `long` correctly; do not accidentally use `Guid` overloads.
- Audit log retention is **2 years** per Phase 2 spec. Add a cleaner BG service (or schedule SQL Agent job) that respects `AnalyticsAuditRedactedDomainEvent` for redaction-on-request (GDPR).
- `AnalyticsOutboxWriter` uses `YallaJo.SharedKernel.Infrastructure.Outbox` namespace (not Abstractions.Outbox).
