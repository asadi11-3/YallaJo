## 2026-05-17

- Used a minimal `TestDbContext` with `InMemoryDatabaseRoot` instead of mocking `DbContext.SaveChangesAsync`, because the unit-of-work logic depends on EF change tracking.
- Verified dispatch ordering by recording markers in the dispatcher callback and the overridden `SaveChangesAsync` method.
## 2026-05-19

- Kept the registry change scoped to the existing dictionary block only; no other logic or imports were changed.
- Removed `SeoMetadataDeletedDomainEvent` and its handler as dead code because no `SeoMetadata` method raises it.
