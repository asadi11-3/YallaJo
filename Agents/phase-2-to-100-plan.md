# Phase 2 → 100% Plan

> **Status:** In progress
> **Phase:** Experience & Discovery (~55 endpoints per `Agents/YallaJo.md`)
> **Current Phase 2 coverage:** ~88% → target 100%
> **Scope note:** External-vendor stubs (push FCM/APNs, search-console ping, Google/DeepL translation) are **intentionally deferred** — same posture as `FakePaymentGateway` in Phase 1. They are **not** treated as gaps.

---

## Context

Audited all 7 Phase-2 module areas (5 parallel explore agents) against the actual codebase. Verdict: Phase 2 is the **strongest** phase — most surfaces are fully built and wired. `ContentPlaces` geo (nearby/map-viewport) and `ContentTours` waypoints/children-info have **zero gaps**. Translation, ContentSeo, Messaging, ContentBlogs CMS, and Analytics are largely complete with a small set of targeted gaps.

Phase-2 spec scope: ContentCore translations, ContentPlaces nearby/map-viewport, ContentTours waypoints/children-info, ContentBlogs CMS+comments+reactions+tour-assoc, ContentSeo metadata/redirects/sitemap/faq/weather, Messaging notifications/devices/templates/support-tickets, Analytics interactions/popular/trending/dashboards. (Analytics recommendations + user preferences are **Phase 4**, not Phase 2.)

---

## Scope Decisions

| Item (gap) | Decision | Rationale |
|---|---|---|
| **G6** Messaging push = `PushNotificationStrategy` stub | **Defer** | Needs FCM/APNs vendor credentials; intentional stub like `FakePaymentGateway` (RISK-015) |
| **G1** ContentSeo `ISearchConsolePinger` only NoOp | **Defer** | External Search Console integration; cosmetic for MVP (RISK-004) |
| Google/DeepL translation providers + provider factory | **Defer** | `AzureTranslateService` already satisfies auto-translate end-to-end; alternates are optional |
| `TranslationStatus.Pending` unused | **Defer (document)** | Workflow is `AutoTranslated → HumanReviewed`; `Pending` is dead enum value, no behavioral gap |
| **G11** ContentBlogs categories/tags routes missing | **Surface to user** | Largest item; may warrant its own sub-plan or deferral — needs a scope decision |

---

## Workstreams

### WS-1 — ContentBlogs `Funny` reaction `[trivial]`

- [ ] **WS-1** **G10** — `ReactionType` enum (`ContentBlogs.Domain/Enums/ReactionType.cs`) has only `Like/Helpful/Insightful`; spec wants `Funny`. Add `Funny = 3`. Verify no exhaustive switch breaks.

### WS-2 — ContentSeo metadata completeness `[quick win]`

- [ ] **WS-2** **G3** — `SeoMetadataEndpoints` has GET/POST/PUT but no `DELETE` and no list. Add `DELETE /metadata/{id}` (`SeoMetadata` + `AppAction.Delete`) and `GET /metadata` list (`SeoMetadata` + `AppAction.Read`) with command/query + handlers.

### WS-3 — Messaging event-consumer gaps (RISK-005) `[in-scope, correctness]`

Follow the `Messaging.Infrastructure/EventHandlers/` `INotificationHandler<IntegrationEventNotification<TEvent>>` pattern.

- [ ] **WS-3a** **G4** — Add `RefundCompleted` consumer that raises a traveler notification (`NotificationType.RefundCompleted` already exists). `RefundInitiatedHandler` + `RefundFailedHandler` exist; `RefundCompleted` does not.
- [ ] **WS-3b** **G5** — Add `finance.dispute-opened.v1` consumer (currently no consumer anywhere) → notify the relevant party.

### WS-4 — ContentSeo weather prefetch loop `[in-scope]`

- [ ] **WS-4** **G2** — `WeatherPreFetchService` is a placeholder (resolves provider + budget gate but the place-selection prefetch loop is unimplemented). Implement the loop: select top places, fetch via `IWeatherProvider`, respect `IWeatherBudgetGate`/`WeatherDailyBudget`. Real `WeatherApiComProvider` already exists (config-gated `Provider=='weatherapi'` + ApiKey).

### WS-5 — Analytics correctness `[in-scope]`

- [ ] **WS-5a** **G7** — Align popularity formula to spec: `bookingCount*3 + reviewCount*2 + avgRating*10 + viewCount*0.1 + favoriteCount*1.5 + recencyBonus` (`recencyBonus = max(0, 50 - daysSinceLastBooking)`) in `PopularityScoreCalculationService`.
- [ ] **WS-5b** **G8** — Interaction ingest queue is lossy: `InteractionIngestQueue` uses `DropOldest` and `RecordInteractionCommandHandler` ignores the `TryEnqueue` result. Make backpressure observable (honor result, log/metric on drop) or switch to a bounded-wait policy.
- [ ] **WS-5c** **G9** — Fix cosmetic data gaps: `GetPopularEntitiesQueryHandler.cs:17` hardcodes `ReviewCount=0`; `AnalyticsDashboardReader.cs:58-66` sets provider-tour `Title = EntityId.ToString()`. Wire real values from snapshots.

### WS-6 — ContentBlogs categories/tags `[largest — pending user decision]`

- [ ] **WS-6** **G11** — No HTTP routes for blog categories/tags under `ContentBlogs.Presentation/Endpoints`. **Surface to user**: confirm whether this is in Phase-2 scope (full entity + endpoints + migration) or deferred.

### WS-7 — Verification `[acceptance gate]`

- [ ] **WS-7** Focused component-level tests for the critical links changed (refund-completed notification persists; popularity score matches spec formula; SEO metadata delete round-trips). Build-verify the full host after each workstream.

---

## Sequencing

1. **WS-1** — trivial enum (`Funny`) ← *starting here*
2. **WS-2** — SEO metadata DELETE/list (isolated endpoint work)
3. **WS-3** — Messaging refund/dispute consumers (correctness, RISK-005)
4. **WS-4** — weather prefetch loop
5. **WS-5** — analytics formula + queue + cosmetic fixes
6. **WS-6** — blog categories/tags (after user scope decision)
7. **WS-7** — verification gate

**Estimate:** ~3–5 days for WS-1…WS-5 + WS-7; WS-6 depends on scope decision.

---

## Definition of Done

Phase 2 = 100% when WS-1 (Funny reaction), WS-2 (SEO metadata completeness), WS-3 (refund/dispute consumers), WS-4 (weather prefetch loop), WS-5 (analytics correctness), and WS-7 (green verification) are complete — with deferrals (G1/G6/Google-DeepL) documented, and WS-6 (G11) resolved per the user's scope decision.
