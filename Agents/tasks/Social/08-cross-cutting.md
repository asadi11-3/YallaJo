# Social — Cross-Cutting Concerns

> Mirrors `Booking/11-cross-cutting.md` and `Finance/10-cross-cutting.md`. Tech Lead enforces at PR review and at the sprint integration freeze (Sun 2026-11-15 17:00).

---

## 1. DI Audit (Social.Infrastructure/DependencyInjection.cs)

| Registration | Symbol | Lifetime |
|---|---|---|
| Pooled DbContext | `SocialDbContext` via `AddDbContextPool` | Scoped |
| DbContext factory | `IDbContextFactory<SocialDbContext>` | Singleton |
| Unit of Work | `ISocialUnitOfWork → SocialUnitOfWork` | Scoped |
| Inbox store | `ISocialInboxStore → SocialInboxStore` | Scoped |
| Outbox writer | `ISocialOutboxWriter → SocialOutboxWriter` | Scoped |
| 6 repositories | `IReviewRepository, IFavoriteRepository, IReportRepository, IContentModerationLogRepository, IBookingEligibilitySnapshotRepository, IEntityRatingCacheRepository` (+ 3 snapshot repos: ITourSnapshotRepository, IPlaceSnapshotRepository, IBusinessSnapshotRepository) | Scoped |
| Moderation services | `IProfanityFilter → BlocklistProfanityFilter` (Scoped), `INsfwClassifier → AlwaysSafeNsfwClassifier` (Singleton) | per above |
| 2 BackgroundServices | OrphanedFavoritesCleanupService, RatingRecalculationService | Singleton (AddHostedService) |
| Permission catalog | `IPermissionCatalog → SocialPermissionCatalog` | Singleton |
| MediatR | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SocialApplicationMarker).Assembly))` | per-call |
| FluentValidation | `services.AddValidatorsFromAssembly(typeof(SocialApplicationMarker).Assembly, ServiceLifetime.Scoped, includeInternalTypes: true)` | Scoped |
| Diagnostics | `SocialDiagnostics` static (ActivitySource "YallaJo.Social" + Meter "YallaJo.Social") | – |
| Cache key + tag builder | `ISocialCacheKeys → SocialCacheKeys` | Singleton |

**Common DI mistakes (fail PR on these):**
- ❌ Registering `IProfanityFilter` as Singleton (it depends on `SocialDbContext` for blocklist reload).
- ❌ Registering `INsfwClassifier` as Scoped (stateless — Singleton is correct).
- ❌ Forgetting `IPermissionCatalog` registration → 19 permissions absent.
- ❌ Forgetting the 3 cross-module snapshot repositories — favorites and reviews won't validate entity types.

---

## 2. Permission Seeder Verification

Expected boot log AFTER this sprint merges (Social = 9th catalog):

```
[INFO] PermissionSeeder discovered 9 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social
[INFO] PermissionSeeder inserted/verified 19 Social permissions
```

Verification SQL: `SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Social.%'` → **19**.

---

## 3. Outbox Type-Registry Parity Test

`tests/Social.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs`:

```csharp
[Fact]
public void All_Social_integration_event_records_are_registered()
{
    var asm = typeof(FavoriteAddedIntegrationEvent).Assembly;
    var declared = asm.GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IIntegrationEvent)) && !t.IsAbstract)
        .ToHashSet();
    var registered = IntegrationEventTypeRegistry.All
        .Where(kvp => kvp.Key.StartsWith("social."))
        .Select(kvp => kvp.Value)
        .ToHashSet();
    declared.Should().BeEquivalentTo(registered);
}
```

Expected 5 logical names:
- `social.review.published.v1`
- `social.review.deleted.v1`
- `social.favorite.added.v1`
- `social.report.resolved.v1`
- `social.rating.recalculated.v1`

Inbox consumers register 4 logical names from upstream modules:
- `booking.tour-booking.completed.v1`
- `content-places.place.deleted.v1`
- `content-places.business.deleted.v1`
- `content-tours.tour.deleted.v1`

Plus the 6 inbound from ContentTours / ContentPlaces snapshot maintenance (Created / Updated / Deleted × 2 entity types).

---

## 4. Build Lock Workaround

Same as Booking. Build only Social-related projects:
```powershell
dotnet build Social/Social.Domain/Social.Domain.csproj
dotnet build Social/Social.Contracts/Social.Contracts.csproj
dotnet build Social/Social.Application/Social.Application.csproj
dotnet build Social/Social.Infrastructure/Social.Infrastructure.csproj
dotnet build Social/Social.Presentation/Social.Presentation.csproj
dotnet build tests/Social.Tests.Unit/Social.Tests.Unit.csproj
dotnet build tests/Social.IntegrationTests/Social.IntegrationTests.csproj
```

---

## 5. Migration Sequence

From `03-entities-matrix.md §9`:

| # | Migration | Owner | When |
|---|---|---|---|
| 1 | SocialAddAggregateRootAndAuditMembers | Tech Lead | PW-2 |
| 2 | SocialAddReviewProvenanceColumns | Mahmoud | T1 |
| 3 | SocialAddReviewReplyTable | Mahmoud | T1 |
| 4 | SocialCreateFavoritesIndexes | Fadwa | T2 |
| 5 | SocialAddReportIndexesAndStatus | Mohammad | T4 |
| 6 | SocialAddContentModerationLog | Mohammad | T4 |
| 7 | SocialAddEntityRatingCacheTable | Mohammad | T5 |
| 8 | SocialAddBookingEligibilitySnapshotTable | Tech Lead | PW-5 |
| 9 | SocialAddCrossModuleSnapshotTables | Tech Lead | PW-5 |
| 10 | SocialSeedProfanityBlocklist | Tech Lead | PW-6 (data migration) |

Apply in order. Tech Lead deploys.

---

## 6. Inbox / Outbox Hygiene

**`CompositeOutboxProcessor`** auto-picks up `SocialDbContext` — verify boot log: `monitoring N DbContexts: ..., SocialDbContext`.

**OutboxCleaner** deletes processed rows older than 7 days. No Social-specific config.

**InboxCleaner** deletes processed rows older than 30 days. No Social-specific config.

**Alerting thresholds:**
- `social.OutboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now - 5 min` count > 100 → page on-call
- Same for InboxMessages

**Per-aggregate guard:** every inbox handler (Booking → Eligibility, ContentPlaces/Tours → Snapshots) MUST use `await inboxStore.HasBeenProcessedAsync(evt.Id, ct)` → work → `inboxStore.MarkAsProcessed(evt.Id)` → ONE SaveChangesAsync.

---

## 7. Profanity Blocklist Management

`social.ProfanityBlocklist` table seeded by `SocialSeedProfanityBlocklist` migration with ~50 English + Arabic entries. Admin updates via direct DB this sprint; UI deferred.

Caching: `BlocklistProfanityFilter` loads blocklist once on first call per scope (per-request), then caches in `IMemoryCache` keyed by language code for 5 minutes. Admin changes take effect within 5 min.

**Adding new words:** Tech Lead runs:
```sql
INSERT INTO social.ProfanityBlocklist (Id, Word, LanguageCode, AddedAt, AddedByUserId)
VALUES (NEWID(), 'badword', 'en', SYSUTCDATETIME(), @adminUserId);
```
Cache invalidates within 5 min OR via admin endpoint `POST /api/v1/admin/social/profanity-cache/clear` (NOT built this sprint).

---

## 8. Cross-Module Coupling Risks

| Risk | Mitigation |
|---|---|
| Booking sends `tour-booking.completed.v1` with userId/targetType pair that doesn't match snapshot reality (e.g., Tour was deleted then re-created with same ID) | Snapshot lookup gracefully returns false → user just doesn't get Verified badge. Reviewer logs warning. |
| ContentTours updates `Tour.AverageRating` from rating-recalculated event, but Tour was deleted in the meantime | Inbox handler in ContentTours guards with `if (tour is null) return Result.Success()` — skip silently. |
| ProfanityFilter blocklist cache stale during admin word addition | Documented 5-min eventual consistency window. Future sprint adds cache-clear endpoint. |
| Bayesian global average drifts when one mega-review batch (e.g., bulk import) skews it | RatingRecalculationService computes globalAvg fresh every day. One-day skew is acceptable; admins can manually trigger via T7 bonus endpoint (deferred) for emergency recalc. |
