# Phase 4 → 100% Plan (Advanced & AI)

> **Status (pre-implementation):** ~20% coverage. User preferences are largely done and Analytics recommendations endpoints exist as 29 routes. Everything else (Finance Discounts CRUD, Messaging Chatbot, Tracking SignalR + sessions) is either NoOp/stubbed or completely missing.
>
> **Author note (m0539 verbatim):** *"now lets go with phase 4 just write the docs do not start implementing"*. This document is the audited, file:line-verified plan. No source code has been touched in this session.

---

## Context

Phase 4 = **Advanced & AI**: discounts, chatbot, live tracking, hybrid recommendations. ~26 endpoints in spec (Agents/YallaJo.md). Audit was performed via 4 parallel `explore` subagents with full file:line verification (see compressed audit synthesis `b68` for raw findings).

The phase is the most heterogenous so far: it spans four unrelated module verticals, each with its own gap profile. The largest single greenfield is **Tracking** (LiveTrackingHub + entire Application layer empty). The deepest correctness gap is **Analytics recommendations** (formula is content-only, no hybrid blend, no cold-start, no Top-20 read clamp, wrong schedule).

Phase 1/2/3 standing items are still open (4 unapplied migrations, un-invoked backfill ops endpoint, stale `endpoint-violations.csv` + module-status table, and a CRITICAL secrets-in-appsettings red flag). They remain Phase-4 closing-summary items.

User constraint (m0036, still active): *"i do not have any payment gateway for now"*. This drives DEFER for any external-vendor work (G7 LLM provider). Drives no other Phase-4 deferrals — discounts, tracking, recommendations all work without a PSP.

---

## Audit summary

### Finance Discounts — partial entity, zero surface

- Domain: `Discount.cs:7-37` exists with rich field set (Code, Name, DiscountType, value, caps, MaxUsageCount, ValidityPeriod, scope, visibility, MaxUsesPerUser, DaysBeforeTour). `DiscountUsage.cs:6-17` audit entity exists. EF config + migration present.
- **Gap:** `DiscountType.cs:3-7` has only `Percentage + FixedAmount` — **missing EarlyBird, GroupDiscount, FlashSale** (spec requires all 5).
- **Gap:** No `DiscountStatus` enum. No lifecycle methods (Activate/Deactivate/Expire). No 50% / 72h cap enforcement. No stacking-rule validator. No domain events. No `DiscountApplication` audit row tied to a successful booking.
- Application layer: **0 commands, 0 queries** (folders empty).
- Presentation: **no `DiscountEndpoints.cs`** anywhere. `FinanceEndpoints.cs:15-40` maps Payment/Invoice/Payout/CommissionRule/ProviderPaymentMethod/Dispute/Earnings — **0 discount routes** vs spec ~8.
- Auth: `FinanceFeatures.cs:7-32` has **no Discount feature constant**. `FinancePermissionCatalog.cs` has 27 permissions, **none discount**.
- Infrastructure: **no `DiscountLifecycleService.cs`**.
- Cross-module: `Booking.Application.Interfaces.IDiscountEvaluator` exists; `Booking.Infrastructure/Services/NoOpDiscountEvaluator.cs:13-18` returns `DiscountEvaluationResult.None`. Registered at `Booking.Infrastructure/DependencyInjection.cs:108`. `CreateTourBookingCommandHandler.cs:186-284` already wires `BookingPricing.DiscountAmount` from evaluator result — so a real evaluator is plug-in-ready.

### Messaging Chatbot — fully deferred greenfield

- Domain entities exist but live under `Messaging.Domain/Entities/_Deferred/`: `ChatBotConversation.cs:5-17` + `ChatBotMessage.cs:5-16`. **No `ChatbotMessageDirection` / `ChatbotConversationStatus` enums.** **No chatbot domain events.**
- **No `IChatbotProvider` abstraction anywhere** under `src/`. `Messaging.Infrastructure.csproj` NuGet refs do not include OpenAI / Anthropic / Claude / any LLM package.
- **No `ChatBotHub.cs`** anywhere. Only `Messaging.Presentation/Hubs/NotificationHub.cs:6-48` exists. Hub mapping template at `MessagingEndpoints.cs:28-30`.
- Rate limiting: `RateLimitPolicies.cs:20-25` has login/otp/refresh/register only — **no chatbot policy** (spec requires 50/user/hr).
- Commands / Queries / Endpoints: **0 files** for `Commands/Chatbot`, `Queries/Chatbot`, `/chatbot` routes.
- Auth: `MessagingFeatures.cs:4-23` has **no Chatbot feature**. Catalog has 18 permissions, none chatbot.
- DB: `MessagingDbContext.cs:16-29` has notifications/tickets/tokens DbSets; chatbot DbSets explicitly commented out as deferred at L25-27. `MessagingDbInitializer.cs:21-22, 44-45` mark chatbot deferred post-MVP.

### Tracking — largest single greenfield in the codebase

- 23 .cs files total. Layer counts: Domain 7, Application **1** (just DI wiring), Infrastructure 12, Presentation 1, Contracts 2.
- Domain (present): `LiveTrackingSession.cs:14-55` (Start/End methods), `LocationSnapshot.cs` (no methods), `TourCheckpoint.cs` (no methods).
- **State machine mismatch:** `SessionStatus.cs:3-9` = `{Active, Paused, Completed, Expired}` but **spec wants `{Scheduled, Active, Completed, Abandoned}`**.
- Events: 2 domain events only (Started, Ended). Missing: location-updated, checkpoint-reached, guide-offline.
- Application layer: **`Commands/Queries/Interfaces` folders all empty (.gitkeep only)**. Only DI wiring exists.
- Infrastructure: **No `LiveTrackingHub.cs` anywhere.** **No `LocationSnapshotPurgeService.cs` anywhere.** No background services. DbContext + EF configs + 3 migrations exist (4 tables).
- Presentation: **`TrackingEndpoints.cs:6-12` is an empty `MapTrackingEndpoints` returning immediately.** **0 HTTP routes.** `Program.cs:393` still calls `app.MapTrackingEndpoints()` (wired-but-empty).
- Contracts: 2 integration events only (Started + Ended). **No `Tracking.Contracts/Authorization/TrackingFeatures.cs` at all** (no Authorization folder). 0 cross-module consumers in the entire repo.

### Analytics Recommendations + Preferences — exists but formula is wrong

- **User preferences: largely DONE.** `UserPreference` + `UserPreferredCategory` + repo + cross-module lookup + Set/Get commands + `UserProfileUpdateJob` (24h, auto-derives from last-90d interactions) + `GdprCleanupJob`. PreferencesEndpoints has GET / + PUT /.
- **RecommendationsEndpoints.cs has 29 routes** verified file:line (incl admin batches/refresh, boosts, editorial pins, seasonality rules, holiday calendar, photogenic flags, experiments, metrics, segments, GDPR delete/export).
- **Formula is content-only, NOT hybrid.** `RefreshSuggestionBatchCommandHandler.cs:49-50` calls only `IRecommendationScoringEngine.Score` (the content scorer `V1ContentSimilarityScorer:25-220` with category/price/proximity/rating/popularity/featured weights + MMR diversity). `ICollaborativeScoringEngine.cs:10-33` is **NoOp**. `IBlendedScoringEngine.cs:11-35` is **NoOp pass-through**. Spec wants `40% collaborative + 35% content + 25% popularity`.
- **No cold-start rule.** Spec wants `<3 interactions → popularity-only`. No interaction-count check in `GetRecommendationsQueryHandler`.
- **Top-20 not enforced on read.** `IRecommendationScoringEngine` default `MaxResults=20` but `GetRecommendationsQuery:6-16 + Handler:20-61` clamps to 100.
- **Schedule wrong.** Spec wants daily 02:00 UTC. Actual scheduler is interval-based 360min (`SuggestionBatchRefreshJob.cs:13-71`). No `RecommendationEngineService.cs` exists.
- **Popularity fallback uses wrong source.** Handler uses raw `EntityAttributeSnapshot.BookingCount + AverageRating` (L67-85), **not `PopularityScore.Score`**.
- Permission catalog thin: `AnalyticsPermissionCatalog.cs:17-19` has only `Recommendation.Read + Preference.Read + Preference.Update`. Missing `Recommendation.Update/Delete + Preference.Delete`. Some routes also use **wrong permission** (e.g. GET `/admin/seasonality/` uses `Batch.Read` instead of `SeasonalityRule.Read`).
- Stale code: `GetSimilarEntitiesQueryHandler.cs:15` has unused `boostRepository` param (pre-existing CS9113 warning).

---

## Scope decisions

| Gap  | Module        | Description                                                                       | Decision  | Workstream |
| ---- | ------------- | --------------------------------------------------------------------------------- | --------- | ---------- |
| G1   | Finance       | Discount admin CRUD endpoints + commands + queries                                | IN-SCOPE  | WS-2       |
| G2   | Finance       | Discount domain enhancements (5 types + lifecycle + caps + stacking + events)     | IN-SCOPE  | WS-1       |
| G3   | Booking↔Finance | Real DiscountEvaluator replacing NoOp                                          | IN-SCOPE  | WS-3       |
| G4   | Finance       | DiscountLifecycleService background service                                       | IN-SCOPE  | WS-4       |
| G5   | Messaging     | ChatBotHub + IChatbotProvider + activate _Deferred entities + DbSet + migration   | IN-SCOPE  | WS-9       |
| G6   | Messaging     | Chatbot commands/queries/endpoints + rate-limit policy + feature/permissions      | IN-SCOPE  | WS-9       |
| G7   | Messaging     | Actual LLM provider integration (OpenAI/Anthropic/Claude)                         | **DEFER** | Phase 4.5  |
| G8   | Tracking      | LiveTrackingHub + SignalR wiring                                                  | IN-SCOPE  | WS-7       |
| G9   | Tracking      | Application layer build-out (commands + queries)                                  | IN-SCOPE  | WS-7 MVP   |
| G10  | Tracking      | 30s snapshot batching + guide-offline detection background service                | **DEFER** | Phase 4.5  |
| G11  | Tracking      | LocationSnapshotPurgeService (daily 04:00 UTC, >30d)                              | IN-SCOPE  | WS-8       |
| G12  | Tracking      | TrackingFeatures + TrackingPermissionCatalog + endpoint stub fill                 | IN-SCOPE  | WS-7       |
| G13  | Tracking      | State-machine fix (Active/Paused/Completed/Expired → Scheduled/Active/Completed/Abandoned) | **DEFER** | Phase 4.5  |
| G14  | Tracking      | Integration events location-updated / checkpoint-reached / guide-offline          | IN-SCOPE  | WS-7       |
| G15  | Analytics     | Hybrid recommendation formula (40% collaborative + 35% content + 25% popularity)  | IN-SCOPE  | WS-5       |
| G16  | Analytics     | Cold-start rule <3 interactions → popularity-only branch                          | IN-SCOPE  | WS-5       |
| G17  | Analytics     | Top-20 read enforcement + RecommendationEngineService daily 02:00 UTC             | IN-SCOPE  | WS-5       |
| G18  | Analytics     | PopularityScore.Score wiring in recommendation fallback                           | IN-SCOPE  | WS-5       |
| G19  | Analytics     | Permission catalog completion + stale boostRepository cleanup + wrong-permission routes fix | IN-SCOPE | WS-6 |

**DEFER rationale:**
- **G7 actual LLM provider** — external vendor work (OpenAI/Anthropic SDK + key + billing + prompt engineering). Pluggable behind `IChatbotProvider` interface. `NoOpChatbotProvider` returns canned response for MVP. Identical pattern to FakePaymentGateway per user constraint m0036.
- **G10 30s batching + offline detection** — complex; depends on G8 hub being operational and producing the right events. Cleaner to ship the hub + happy-path events first, then layer the consumer/batcher in Phase 4.5 once we can observe real traffic patterns.
- **G13 Tracking state-machine fix** — entity refactor + EF migration + data migration for any in-flight sessions. Current shape (Active/Paused/Completed/Expired) is functional for MVP; the spec mismatch is a naming + Scheduled-pre-start gap, not a correctness gap.

---

## Workstreams

### WS-1 — Finance Discount domain enhancements (G2)
- [ ] Expand `DiscountType` enum: add `EarlyBird`, `GroupDiscount`, `FlashSale`.
- [ ] Add `DiscountStatus` enum (`Draft`, `Active`, `Paused`, `Expired`).
- [ ] Add `Discount.Activate()` / `Pause()` / `Expire()` lifecycle methods + domain events.
- [ ] Add 50% cap enforcement for `Percentage` type (validation).
- [ ] Add 72h cap enforcement for `FlashSale` type (validation).
- [ ] Add stacking-rule validator: max `1 Percentage + 1 FixedAmount` **OR** `1 EarlyBird`. (Spec §discount.)
- [ ] Add domain events: `DiscountCreatedDomainEvent`, `DiscountActivatedDomainEvent`, `DiscountExpiredDomainEvent`, `DiscountAppliedDomainEvent`.
- [ ] Add `DiscountApplication` audit entity (FK to Booking + Discount, snapshot amount applied).
- [ ] EF config + migration `AddDiscountStatus_DiscountApplication`.
- [ ] Build green.

### WS-2 — Finance Discount admin CRUD (G1)
- [ ] `FinanceFeatures.Discount` constant.
- [ ] `FinancePermissionCatalog` entries: `Discount.{Create, Read, Update, Delete, Activate}`.
- [ ] Commands: `CreateDiscount`, `UpdateDiscount`, `ActivateDiscount`, `ExpireDiscount`, `DeleteDiscount` (soft).
- [ ] Queries: `GetDiscountById`, `ListDiscounts(filterByStatus, byProvider, byScope)`, `GetActiveDiscountsForTour(tourId, userId)`.
- [ ] `DiscountEndpoints.cs`: POST/PUT/POST activate/POST expire/DELETE/{id} (admin); GET / + GET /{id} (admin); GET /active?tourId= (public).
- [ ] Wired into `FinanceEndpoints.cs` MapGroup.
- [ ] Build green.

### WS-3 — Real Booking → Finance DiscountEvaluator (G3)
- [ ] Decide cross-module pattern: either (a) Booking calls `Finance.Contracts/Services/IDiscountLookupService.cs` (sync read) **or** (b) Finance publishes `DiscountAvailableSnapshot` events to Booking (eventual consistency). Recommendation: (a) — same shape as commission lookup.
- [ ] If (a): add `IDiscountLookupService` in `Finance.Contracts/Services/`, register in Finance.Infrastructure DI, inject into Booking.
- [ ] Implement `RealDiscountEvaluator : IDiscountEvaluator` in `Booking.Infrastructure/Services/` (replaces `NoOpDiscountEvaluator`).
- [ ] Validates: discount is `Active`, within `ValidityPeriod`, applies to `TargetScope`, respects `MaxUsageCount` + `MaxUsesPerUser`, respects `DaysBeforeTour`.
- [ ] Stacking: returns `DiscountEvaluationResult` with possibly **two** discounts (1 percentage + 1 fixed) when stack allowed.
- [ ] DI registration in `Booking.Infrastructure/DependencyInjection.cs:108`.
- [ ] Component test: 5 cases (single percentage, single fixed, stacked %+fixed, exclusive earlybird, no-match).
- [ ] Build green.

### WS-4 — DiscountLifecycleService (G4)
- [ ] `Finance.Infrastructure/BackgroundServices/DiscountLifecycleService.cs` extending `BackgroundService`.
- [ ] 30 min interval (configurable `DiscountLifecycleOptions.IntervalMinutes` default 30).
- [ ] Pages through `Discounts.Where(Status==Active && ValidityPeriod.End <= UtcNow)` in batches; calls `discount.Expire()`; saves.
- [ ] Logging + counter (`expiredCount`).
- [ ] Registered in `Finance.Infrastructure/DependencyInjection.cs`.
- [ ] Build green.

### WS-5 — Analytics hybrid recommendation formula (G15+G16+G17+G18)
- [ ] Implement real `CollaborativeScoringEngine` replacing NoOp. Algorithm: cosine similarity on `UserInteraction` vectors (last-90d), top-K similar users (K=20), weight item scores by similarity.
- [ ] Implement real `BlendedScoringEngine` replacing pass-through. Weights: 0.40 collaborative + 0.35 content + 0.25 popularity. Normalize each component to [0..1] before blending.
- [ ] Add cold-start branch in `GetRecommendationsQueryHandler`: if `interactionCount(userId, last-90d) < 3`, skip collaborative + content, use `IPopularityScoreRepository.GetTopByTypeAsync` only.
- [ ] Wire `PopularityScore.Score` (not `EntityAttributeSnapshot.BookingCount`) as the popularity component.
- [ ] Top-20 read enforcement: clamp `GetRecommendationsQuery.MaxResults` to 20 (currently 100).
- [ ] New `RecommendationEngineService.cs` background service: daily 02:00 UTC schedule (replaces / supplements `SuggestionBatchRefreshJob`'s 360-min cadence). Batches of 100 users per cycle.
- [ ] Component tests: cold-start fallback, blended-score math sanity (sum of weights = 1), Top-20 clamp.
- [ ] Build green.

### WS-6 — Analytics permission catalog completion (G19)
- [ ] Add to `AnalyticsPermissionCatalog`: `Recommendation.Update`, `Recommendation.Delete`, `Preference.Delete`.
- [ ] Fix wrong-permission routes (verified examples):
  - GET `/admin/seasonality/` uses `Batch.Read` → should be `SeasonalityRule.Read`.
  - GET `/admin/holidays/{year}` similarly.
  - (Sweep all 29 routes against feature catalog; correct any mismatches found.)
- [ ] Remove stale `boostRepository` param from `GetSimilarEntitiesQueryHandler.cs:15` (clears CS9113 warning).
- [ ] Build green.

### WS-7 — Tracking MVP (G8 + G9 MVP + G12 + G14)
- [ ] `Tracking.Contracts/Authorization/TrackingFeatures.cs` with `LiveSession`, `LocationSnapshot`, `Checkpoint` constants.
- [ ] `TrackingPermissionCatalog.cs` with `LiveSession.{Read, Start, End, Join}`, `LocationSnapshot.{Read, Send}`, `Checkpoint.{Reach}`.
- [ ] `Tracking.Presentation/Hubs/LiveTrackingHub.cs` ([Authorize], 6 hub methods: `SendLocation(lat, lng, accuracy, speed?, heading?, altitude?)`, `ReachCheckpoint(checkpointId, notes?)`, `JoinSession(sessionId)`, `LeaveSession(sessionId)`, `StartSession(bookingId)`, `EndSession(sessionId)`; client methods: `LocationUpdate`, `CheckpointReached`, `SessionStarted`, `SessionEnded`, `GuideOffline`; group `session:{id}`).
- [ ] Hub mapping via `TrackingEndpoints.cs`: `endpoints.MapHub<LiveTrackingHub>("/hubs/live-tracking")`.
- [ ] Application commands (minimal MVP):
  - `StartLiveTrackingSessionCommand(bookingId, tourGuideId)` → calls `LiveTrackingSession.Start()` + saves.
  - `EndLiveTrackingSessionCommand(sessionId)` → calls `LiveTrackingSession.End()` + saves.
  - `RecordLocationSnapshotCommand(sessionId, location, accuracy, speed?, heading?, altitude?)` → adds `LocationSnapshot` + updates session `LastLocationUpdate`.
  - `ReachCheckpointCommand(sessionId, waypointId, notes?)` → sets `Checkpoint.Status = Reached`.
- [ ] Application queries: `GetLiveSessionByBookingIdQuery`, `GetSessionLocationHistoryQuery`.
- [ ] Domain events: `LocationSnapshotRecordedDomainEvent`, `CheckpointReachedDomainEvent`, `GuideOfflineDomainEvent`.
- [ ] Integration events (in `Tracking.Contracts/IntegrationEvents/`): `LiveTrackingLocationUpdatedIntegrationEvent`, `LiveTrackingCheckpointReachedIntegrationEvent`, `LiveTrackingGuideOfflineIntegrationEvent`. Register in `IntegrationEventTypeRegistry.cs`.
- [ ] HTTP routes (slim): GET `/api/v1/tracking/sessions/{bookingId}` (booking participant), GET `/api/v1/tracking/sessions/{id}/locations` (participant), POST `/api/v1/tracking/sessions/{id}/end` (guide/admin).
- [ ] Build green.

### WS-8 — Tracking LocationSnapshotPurgeService (G11)
- [ ] `Tracking.Infrastructure/BackgroundServices/LocationSnapshotPurgeService.cs` daily at 04:00 UTC.
- [ ] Deletes `LocationSnapshots.Where(CapturedAt < UtcNow - 30d)` in batches of 1000.
- [ ] Logging + counter.
- [ ] Registered in DI.
- [ ] Build green.

### WS-9 — Messaging Chatbot MVP (G5 + G6 minus G7)
- [ ] Activate `_Deferred` entities: move `ChatBotConversation.cs` + `ChatBotMessage.cs` out of `_Deferred/`, add lifecycle methods (Start/AddMessage/Close), add domain events.
- [ ] Add enums: `ChatbotMessageDirection {UserToBot, BotToUser, System}`, `ChatbotConversationStatus {Open, Closed, HandedOff}`.
- [ ] Add `IChatbotProvider` abstraction in `Messaging.Application/Interfaces/`: `Task<ChatbotResponse> CompleteAsync(ConversationContext context, string userMessage, CancellationToken ct)` where `ConversationContext` carries last 10 messages.
- [ ] Add `NoOpChatbotProvider` implementation returning canned `"Sorry, I'm currently offline. A support agent will follow up."` response (NoOp like FakePaymentGateway).
- [ ] DbSet<ChatBotConversation> + DbSet<ChatBotMessage> uncommented in `MessagingDbContext`; EF configurations; migration `AddChatbotConversations`.
- [ ] `MessagingFeatures.Chatbot` constant + permissions (`Conversation.{Read, Send, Close}`).
- [ ] Add `chatbot-rate-limit` to `RateLimitPolicies.cs`: 50 messages / user / hour.
- [ ] `ChatBotHub` in `Messaging.Presentation/Hubs/` ([Authorize], rate-limited by policy): hub methods `SendMessage(conversationId, text)`, `CloseConversation(conversationId)`, `RateResponse(messageId, stars)`; client methods `ReceiveMessage`, `StreamChunk`, `TypingIndicator`, `HandoffToHuman`, `ConversationClosed`.
- [ ] Hub mapping in `MessagingEndpoints.cs`: `MapHub<ChatBotHub>("/hubs/chatbot")`.
- [ ] HTTP routes (read-only history surface): GET `/api/v1/chatbot/conversations`, GET `/api/v1/chatbot/conversations/{id}`.
- [ ] Conversation context = last 10 `ChatBotMessage` rows for that user, sorted ascending.
- [ ] Build green.

### WS-10 — Component tests + full build verify
- [ ] Discount tests: 5 cases for `RealDiscountEvaluator` (single %, single fixed, stacked %+fixed, exclusive earlybird, no-match). Edit-window / 50%-cap / 72h-cap entity tests.
- [ ] Hybrid recommendation: cold-start branch test, blended-score math sanity, Top-20 clamp.
- [ ] Tracking MVP: `LiveTrackingSession.Start()` / `End()` state machine tests; `RecordLocationSnapshotCommand` handler test (in-memory DbContext) verifying snapshot persists + session `LastLocationUpdate` updated + domain event raised.
- [ ] Chatbot MVP: `NoOpChatbotProvider.CompleteAsync` returns canned response; rate-limit policy resolves.
- [ ] Full `dotnet build src\Hosts\YallaJo.Api\YallaJo.Api.csproj` → 0 errors.
- [ ] Phase-1 regression: `TourSnapshotReaderRoundTripTests` still 2/2 green; Phase-3 `TourBookingDisputeStateMachineTests` still 5/5 green; all new Phase-4 tests pass.

---

## Sequencing

Recommended order (parallelizable where indicated):

1. **WS-1, WS-2, WS-3, WS-4** (Finance Discounts — work together since they share `Discount` entity + cross-module evaluator) — 2.5 days.
2. **WS-5, WS-6** (Analytics formula refactor + catalog cleanup — independent of Finance) — can run in parallel with the Finance bucket — 2 days.
3. **WS-7, WS-8** (Tracking MVP + purge service) — largest workstream, sequenced after Analytics since both touch background services and we don't want two BG-service edits in the same build cycle — 2 days.
4. **WS-9** (Chatbot MVP with NoOp provider) — independent; ship last so any LLM-provider decision can be made post-MVP — 1.5 days.
5. **WS-10** (verification gate) — 0.5 days.

**Estimated total: ~8.5 working days** (Phase 1: 5d, Phase 2: 5d, Phase 3: 7d for reference).

---

## Definition of Done

A Phase 4 → 100% delivery is complete when:

- [ ] All IN-SCOPE workstreams (WS-1 through WS-10) report build-green.
- [ ] All 5 spec discount types are valid `DiscountType` enum members and ship with cap/stacking enforcement.
- [ ] `RealDiscountEvaluator` is registered in Booking DI in place of `NoOpDiscountEvaluator`. RISK-008 closed.
- [ ] Recommendation read path uses real `BlendedScoringEngine` with 40/35/25 weights, cold-start branch active, Top-20 clamp, daily 02:00 UTC schedule, `PopularityScore.Score` as popularity source.
- [ ] `LiveTrackingHub` is mapped at `/hubs/live-tracking` with the 6 hub methods + 5 client methods. RISK-010 partially closed (LiveTrackingHub side).
- [ ] `ChatBotHub` is mapped at `/hubs/chatbot` with `NoOpChatbotProvider`. RISK-010 fully closed (chatbot side). RISK-015 partially closed (chatbot stub now functional skeleton).
- [ ] All new Phase-4 tests pass; Phase-1/2/3 regression tests still pass.
- [ ] **Standing items re-surfaced** to user in final delivery:
  1. 🚨 **CRITICAL SECURITY**: real Production secrets in 3 `src/Hosts/YallaJo.Api/appsettings*.json` (JWT key, Gmail AppPassword, reCAPTCHA, ExternalAuth signing, DB conn strings). Rotate + move to user-secrets / Key Vault.
  2. 4 EF migrations still NOT DB-applied: `20260605200222_AddMaxGroupSizeToTourSnapshot` (Phase 1), `20260606110439_AddTourPackageApprovalStatus` (WS-5a), `20260606111615_AddBookingDispute` (WS-3a), `20260606114147_AddAccessibilityReviews` (WS-2). Plus all new Phase-4 migrations.
  3. Phase-1 ops backfill `POST /api/v1/ops/content-tours/backfill/tour-snapshots` not invoked in Prod.
  4. Stale docs: `Agents/endpoint-violations.csv` + module-status table in `Agents/YallaJo.md` (RISK-006).

---

## Phase 4.5 candidates (DEFER list)

To revisit after Phase 4 IN-SCOPE ships:

| Gap | Description | Why deferred |
| --- | --- | --- |
| G7 | LLM provider integration (OpenAI/Anthropic/Claude SDK + key + billing + prompts) | External vendor; `NoOpChatbotProvider` ships as the stub like FakePaymentGateway. Pluggable. |
| G10 | 30s snapshot batching + guide-offline detection background service | Complex consumer behaviour; cleaner to ship hub + happy-path events first, then layer this on observed traffic. |
| G13 | Tracking state-machine fix (Scheduled/Abandoned naming) | Entity refactor + EF migration + data migration. Spec mismatch, not a correctness gap. |
| Phase 3.5 G3c | TourPackagePricingTier | Carried from Phase 3. |
| Phase 3.5 G4b | PackageBooking aggregate (full cross-module ingestion mirror of Phase-1 TourSnapshot work) | Huge; carried from Phase 3. |
| Phase 3.5 G5a | Finance Subscriptions (recurring billing infra) | Requires PSP. User m0036 'no payment gateway'. |
| Phase 3.5 G5b | Finance Loyalty (1pt/JOD, FIFO 12mo, 100pts=1JOD) | Complex standalone system. |
| Phase 3.5 G5c | Finance Referrals | Depends on Loyalty. |
| Phase 3.5 — | WS-2 admin moderation queue for accessibility-reviews | Carried from Phase 3 MVP scope choice. |
| Phase 3.5 — | Booking-eligibility verified-review gate for accessibility-reviews | Same. |

---

## Open questions for user (before implementation starts)

1. **G7 LLM provider**: confirm DEFER. `NoOpChatbotProvider` ships now; you wire OpenAI/Anthropic key later.
2. **G10 30s batching**: confirm DEFER for Phase 4. Hub will publish the events; consumer/batcher comes after observation.
3. **G13 SessionStatus naming**: confirm DEFER. Current `{Active, Paused, Completed, Expired}` is functional. Spec wants `{Scheduled, Active, Completed, Abandoned}` — naming + a pre-start state. Defer means accepting current names for MVP.
4. **WS-3 cross-module pattern**: confirm preferred shape — (a) sync `IDiscountLookupService` in `Finance.Contracts` (recommended; mirrors `ICommissionLookupService` pattern) **vs.** (b) Booking-owned `DiscountSnapshot` table fed by Finance integration events (eventual consistency, heavier infra).
5. **WS-9 conversation history retention**: confirm policy. Default proposal: keep all messages indefinitely (no purge job); add purge in Phase 4.5 if storage grows.

A "go ahead, all defaults" reply is welcome — defaults are: DEFER G7/G10/G13, use pattern (a) for WS-3, keep chatbot history indefinitely.
