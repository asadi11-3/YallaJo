## 2026-05-25 — W1-D CQRS bypass removal

- Analytics.Presentation admin recommendation endpoints now depend on `ISender` only for the 15 formerly repository-backed routes.
- Admin read DTOs for suggestion batches, seasonality rules, and holiday calendars live in their query files so Presentation keeps only request models.
- `dotnet build "YallaJo.sln" --no-restore` succeeds after the refactor; existing analyzer/package warnings remain.
