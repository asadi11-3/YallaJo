## 2026-05-18 — ContentSeo Weather PDF §11

- Initial `ContentSeo.Infrastructure` pre-migration build failed because `ContentSeoDbInitializer` still referenced removed `WeatherCache.Forecast`. Fixed by seeding `ForecastJson`, rounded coordinates, and `ForecastDate`.
- EF migration `20260518223144_WeatherDailyBudgetAndCoordinateKey` also captured existing `FaqItems.Question` length drift (`nvarchar(1000)` -> `nvarchar(500)`) from current model configuration, unrelated to the weather code but required for snapshot alignment.
