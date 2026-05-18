## 2026-05-17

- `UnitOfWork<TContext>` domain-event dispatch can be tested cleanly with `EFCore.InMemory` plus a tiny derived `DbContext` that overrides `SaveChangesAsync` to record call order.
- `ChangeTracker.Entries<IAggregateRoot>()` correctly picks up tracked aggregate entities in the test harness when the entity type implements `IAggregateRoot` directly.
- `Microsoft.EntityFrameworkCore.InMemory` was already referenced in `tests/SharedKernel.Tests.Unit.csproj`; no package change was needed.
## 2026-05-19

- Added 4 ContentSeo integration event aliases to `IntegrationEventTypeRegistry` without touching imports.
- Verified orphan `SeoMetadataDeletedDomainEvent` references were removed entirely after deleting the handler and domain event files.
