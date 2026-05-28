## 2026-05-25 — Admin analytics CQRS boundary

- Added command/query handlers in `Analytics.Application` instead of allowing Presentation to create/update aggregates directly. Handlers own repositories, unit of work, cache invalidation, and logging.
- Query records implement `ICacheableQuery` with narrow admin cache tags; mutating command handlers invalidate matching tags after successful save.
