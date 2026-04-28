# Session Learnings — Mohammad Task 2 + Task 3

**Session**: ses_234f4634bffen4OPa7ZcsrDBlt
**Completed**: 2026-04-26
**Build result**: ✅ 0 errors

---

## Architecture discoveries

### Namespace collision pattern
When a command lives in `ContentTours.Application.Commands.TourSchedule.CreateTourSchedule`,
the namespace `TourSchedule` shadows the entity type `TourSchedule`.
**Fix**: use fully qualified `ContentTours.Domain.Entities.TourSchedule` in those files.
Same applies to `TourPricingTier`, `Tour` namespaces.

### Application layer cannot use DbContext directly
Application project only references Domain + Contracts + SharedKernel.Application.
For outbox writes from Application handlers, use `IContentToursOutboxWriter` interface
(pattern from ContentPlaces: `IContentPlacesOutboxWriter`).

### Two UoW interfaces needed
- `IContentToursUnitOfWork` — plain `context.SaveChangesAsync()`, no event dispatch.
  Use for non-aggregate handlers (TourSchedule, TourPricingTier).
- `IContentToursEventUnitOfWork` — wraps `IUnitOfWork<ContentToursDbContext>` which
  dispatches domain events before SaveChanges. Use for Tour aggregate mutations (ToggleTourFeatured).

### EfEntityRepository for non-aggregates
`EfEntityRepository<TEntity, TKey>` (SharedKernel.Infrastructure) is the base for
non-aggregate repos. `EfRepository<TEntity, TKey>` requires `IAggregateRoot`.

### IReadRepository.Query() for IQueryable access
`tourRepo.Query(asNoTracking: true)` returns `IQueryable<Tour>` — needed for
SearchToursQueryHandler to build dynamic LINQ filters + EF ToListAsync.
Required adding `Microsoft.EntityFrameworkCore` package to Application project.

### AppAction is a static class, not enum
`AppAction.Feature`, `AppAction.ReadOwn`, `AppAction.ReadAny` were missing.
Added them to `YallaJo.SharedKernel.Application/Authorization/AppAction.cs`.

### Outcome enum has no 422
`Outcome.UnprocessableEntity` does not exist. Brief's 422 errors
(`OverlapDetected`, `ExpansionTooLarge`, `CustomDateOutOfRange`) mapped to `Outcome.Conflict` (409).
Raise with tech lead to add 422 or confirm 409 is acceptable.

### TourStatus enum mismatch
Current enum: `Draft=0, Published=1, Archived=2, Suspended=3`
Brief expects: `Draft=0, Pending=1, Approved=2, Rejected=3, Suspended=4, Archived=5`
Adult-tier guard uses `Published` as proxy for `Approved`.
Mahmoud's Task 1 PW-1 migration must rename this before Task 2B is fully correct.

### TimeOnly EF conversion
`TourScheduleConfiguration` was missing `HasConversion(v => v.ToTimeSpan(), v => TimeOnly.FromTimeSpan(v))`.
Added + created migration `AddTourScheduleTimeOnlyConversionAndIndex`.

### IntegrationEventTypeRegistry is central
All new integration events MUST be registered in
`YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`
before `OutboxMessage.Create()` can be called (it calls `GetName()` which throws if unregistered).

### Contracts project structure
Integration events live at root of `ContentTours.Contracts` (namespace `ContentTours.Contracts`),
not in a subfolder. Matches existing `PlaceTourCountUpdatedIntegrationEvent.cs` pattern.

---

## Files created/modified (summary)

### New files (47)
- Domain: TourFeaturedChangedDomainEvent.cs
- Domain/Repositories: ITourRepository, ITourScheduleRepository, ITourPricingTierRepository
- Application/Commands: 11 TourSchedule + 11 TourPricingTier + 3 Tour = 25 files
- Application/Queries: 4 TourSchedule + 4 TourPricingTier + 18 Tour = 26 files
- Application/Interfaces: IScheduleBookingCountService, IContentToursOutboxWriter, IContentToursEventUnitOfWork
- Application/Caching: TourScheduleCacheKeys, TourPricingTierCacheKeys, TourCacheKeys, TourSearchCacheKeys
- Infrastructure/Repositories: TourRepository, TourScheduleRepository, TourPricingTierRepository
- Infrastructure/Services: NoOpScheduleBookingCountService
- Infrastructure/Persistence: ContentToursOutboxWriter, ContentToursEventUnitOfWork
- Infrastructure/EventHandlers: TourFeaturedChangedDomainEventHandler
- Contracts: TourScheduleChangedIntegrationEvent, TourPricingTierChangedIntegrationEvent, TourFeaturedChangedIntegrationEvent
- Contracts/Authorization: ContentToursFeatures, ContentToursPermissionCatalog
- Presentation: TourScheduleEndpoints, TourPricingTierEndpoints, TourSearchEndpoints

### Modified files (10)
- AppAction.cs — added Feature, ReadOwn, ReadAny
- Tour.cs — added SetFeatured()
- TourSchedule.cs — added Create/Update/Deactivate
- TourPricingTier.cs — added Create/Update/Deactivate/IsAdult
- TourScheduleConfiguration.cs — TimeOnly conversion + composite index
- IntegrationEventTypeRegistry.cs — 3 new events
- ContentTours.Infrastructure/DependencyInjection.cs — repos + outbox + UoW + permissions
- ContentTours.Contracts.csproj — added SharedKernel.Application ref
- ContentTours.Application.csproj — added EF Core package
- ContentTours.Presentation.csproj — added Contracts + SharedKernel.Presentation refs
- ContentToursEndpoints.cs — chained all 3 endpoint groups

### Migrations
- `AddTourScheduleTimeOnlyConversionAndIndex` — TimeOnly → time(7) + composite index

---

## Known deviations / open items for Mohammad

1. **TourStatus enum** — `Published` used where brief says `Approved`. Blocked on Mahmoud's PW-1 migration.
2. **422 → 409** — `OverlapDetected`, `ExpansionTooLarge`, `CustomDateOutOfRange` return 409 not 422.
3. **SuggestTours** — uses in-memory `StartsWith` via `GetAllAsync`. For p95 < 100ms at scale, add `IX_Tours_Status_IsDeleted_Name` filtered index.
4. **Search ranking** — simplified (no LOG10 score, no recency decay in SQL). Uses popularity sort as fallback. Full relevance scoring deferred to v2.
5. **Translation join in Search** — not implemented (no `TourTranslation` join in search/suggest). Deferred to v2.
6. **`TourScheduleChangedIntegrationEvent`** — shipped (Booking team can consume). Registry entry added.
