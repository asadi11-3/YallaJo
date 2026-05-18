## 2026-05-18 — ContentSeo Weather PDF §11

- `WeatherDailyBudget` must implement `IAggregateRoot` when using `EfRepository<TEntity, Guid>` because the shared EF repository is constrained to aggregate roots.
- This repo does not currently have `IIntegrationEventPublisher`; ContentSeo domain-event handlers publish integration events by staging `OutboxMessage.Create(...)` on the module DbContext and saving via the module UoW.
- `WeatherOptions` uses `DailyBudget` as the configured daily limit property; the budget gate reads this value and clamps it to at least 1.
