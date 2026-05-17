## 2026-05-17

- Used a minimal `TestDbContext` with `InMemoryDatabaseRoot` instead of mocking `DbContext.SaveChangesAsync`, because the unit-of-work logic depends on EF change tracking.
- Verified dispatch ordering by recording markers in the dispatcher callback and the overridden `SaveChangesAsync` method.
