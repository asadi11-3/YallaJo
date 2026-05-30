# Playwright MCP Test Scenarios — Booking Module

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans / Module score

- Primary inputs read: `Agents/Plans/Booking-Workflow.md`, `Booking-Audit-Report.md`, `Booking-FixPlan.md`.
- Reference inputs read for Booking-relevant rows: `Master-RoadmapTo10.md`, `CrossDocumentAnalysisReport.md`.
- Code sampled against current repository root (`Booking.Application`, `Booking.Domain`, `Booking.Infrastructure`, `Booking.Presentation`). User-provided `src\...` paths do not exist in this checkout.
- Current score baseline: audit says **7.2/10**; `Master-RoadmapTo10.md` targets **8.5 → 10.0** for Booking W3-A. Current code is newer than the audit in several areas: bulk slot creation, `BookingReminderService`, `SlotCleanupService`, and ContentTours suspension/deletion handlers now exist.
- Test mode: Playwright MCP drives browser/Swagger/UI pages and uses `browser_evaluate` for authenticated `fetch` calls where Razor UI is missing or incomplete.

## 0. Prerequisites (DB, seed, recaptcha disabled, slot data, payment gateway stub)

1. Fix local connection string before starting API/Web: `appsettings.Development.json` currently has `Server=MOHAMMAD\SQLEXPRESS\\SQLEXPRESS`; expected one SQL Server instance name.
2. Start API at `https://localhost:57065`; verify `mcp__playwright__browser_navigate("https://localhost:57065/swagger")` succeeds and `browser_console_messages` has no fatal errors.
3. Start Web at `https://localhost:57065/swagger`; note this checkout does **not** contain `YallaJo.Web/Areas/Booking`, so UI tests should fall back to Swagger/API fetch until the Razor area exists.
4. Seed users: `admin@yallajo.test`, `userA@yallajo.test`, `userB@yallajo.test`, `guide-approved@yallajo.test`, `business@yallajo.test`, `agency@yallajo.test`, `suspended@yallajo.test`; password `TestPass!23`.
5. Recaptcha is globally disabled/commented out; do not include Recaptcha fields or assertions.
6. Seed at least:
   - one active approved instant tour with provider/guide, adult pricing, refund tiers, future slot > 24h, max capacity >= 4;
   - one non-instant tour/slot for PendingConfirmation tests;
   - one slot within 2h for lead-time rejection;
   - one nearly-full slot for capacity overflow;
   - one confirmed non-private booking with remaining capacity for join-request tests;
   - one booking in each status used by state-machine tests.
7. Payment remains Finance-owned. Booking returns `PaymentToken = "PENDING_FINANCE_INTEGRATION"`; tests should treat real card redirect as **NOT_BUILT** and use Finance test gateway/webhook for payment completion where available.
8. Playwright MCP tools to use: `browser_navigate`, `browser_snapshot`, `browser_click`, `browser_fill_form`, `browser_select_option`, `browser_press_key`, `browser_wait_for`, `browser_network_requests`, `browser_evaluate`, `browser_take_screenshot`, `browser_console_messages`, `browser_tabs`.

## 1. Built — Active Test Scenarios

> Base API route from current code: `/api/v1/booking`; admin force-refund: `/api/v1/admin/bookings`; join requests: `/api/v1/booking/join-requests`; guide discounts: `/api/v1/booking/guide-discounts`. Every scenario is tagged with workflow section and command/query class.

| ID | Endpoint | Command/Query class | Workflow § | Role | MCP steps | Assertions / edge cases |
|---|---|---|---|---|---|---|
| TC-BK-001 | `GET /api/v1/booking/availability/{tourId}` | `GetAvailabilityForTourQuery` | Phase B, Endpoints Availability | Anonymous | `browser_navigate` Swagger; `browser_evaluate` GET active seeded tour availability; `browser_network_requests` inspect response. | 200; page grouped by date; `availableCount = maxCapacity - bookedCount - lockedCount`; cursor/pageSize honored; no auth token required. |
| TC-BK-002 | `GET /api/v1/booking/availability/{tourId}/{date}` | `GetAvailabilityForTourOnDateQuery` | Phase B | Anonymous | GET seeded date; repeat with date containing no slots. | 200 with slots for date; empty/zero slots for no-slot date; 404 only for missing/inactive tour if implementation returns not found. |
| TC-BK-003 | `POST /api/v1/booking/availability/slots` | `CreateAvailabilitySlotCommand` | Phase A, Phase 8 | Guide/Admin | Login as `guide-approved`; POST future date/time/capacity. | 201; returned `AvailabilitySlotDto`; auth required; validates `TourId`, date, start < end, capacity > 0; conflict on duplicate overlapping slot. |
| TC-BK-004 | `POST /api/v1/booking/availability/slots/bulk` | `CreateBulkAvailabilitySlotsCommand` | Phase A, FixPlan Fix 7 | Guide/Admin | Login guide; POST weekly/custom recurrence with `DaysOfWeek=["Mon","Wed"]`; inspect network. | 200 with created/skipped counts; invalid recurrence -> 400/422 `AvailabilitySlot.InvalidRecurrence`; custom without days -> `AvailabilitySlot.InvalidDaysOfWeek`; `SkipExisting=true` avoids duplicates. |
| TC-BK-005 | `PUT /api/v1/booking/availability/slots/{id}` | `UpdateAvailabilitySlotCommand` | Phase A, concurrency | Guide/Admin | GET/record rowVersion; PUT new `MaxCapacity`; repeat using stale rowVersion from first read. | First 200 with changed capacity; stale second update -> 409 `AvailabilitySlot.ConcurrencyConflict` or equivalent; cannot set capacity below booked/locked count. |
| TC-BK-006 | `DELETE /api/v1/booking/availability/slots/{id}?rowVersion=...` | `DeleteAvailabilitySlotCommand` | Phase A cleanup | Guide/Admin | Create disposable slot; DELETE with valid rowVersion; GET availability. | 204; slot no longer active/listed; stale/invalid rowVersion path returns 409 or guarded failure; cannot delete slot with active booking if domain forbids. |
| TC-BK-007 | `POST /api/v1/booking/tour` | `CreateTourBookingCommand` | Phase C | Customer | Login `userA`; POST active future slot with `{adult:1}`. | 201; status `AwaitingPayment`; reference matches `YJ-YYYYMM-XXXX` or current code `YJ-YYYYMMDD-XXXXXX` (record divergence); `PaymentExpiresAt` ≈ now+10m; total >= 5 JOD; line items present; network response no Recaptcha. |
| TC-BK-008 | `POST /api/v1/booking/tour` | `CreateTourBookingCommandValidator` | Phase C validation | Customer | POST empty GUIDs, null participant breakdown, adult=0, child=-1, promo lowercase/too long, specialRequests > 2000. | 400 validation; errors for TourId, AvailabilitySlotId, ParticipantBreakdown, adult >=1, non-negative child/infant/senior, total <=100, promo `[A-Z0-9]`, specialRequests length. |
| TC-BK-009 | `POST /api/v1/booking/tour` | `CreateTourBookingCommandHandler` | Phase C #1-3 | Customer | Use inactive/missing tour; use slot for different tour; use suspended provider seed. | 404 `Tour.NotFound`; 404 `AvailabilitySlot.NotFound`; 409 `Booking.ProviderSuspended`. |
| TC-BK-010 | `POST /api/v1/booking/tour` | `CreateTourBookingCommandHandler` | Phase C #4 | Customer | Try slot starting in <2h. | 400/422 invalid `TourBooking.TooEarly`; message says bookings require at least 2h lead time. |
| TC-BK-011 | `POST /api/v1/booking/tour` | `CreateTourBookingCommandHandler` | Phase C #5 | Customer | As `userA`, create 3 AwaitingPayment bookings; attempt 4th. | First three 201; fourth 409 `TourBooking.ConcurrentLimit`; cleanup expires/cancels created bookings. |
| TC-BK-012 | `POST /api/v1/booking/tour` | `CreateTourBookingCommandHandler` | Phase C #6 | Customer | Create booking for same user/tour/date; repeat same tour/date different slot if seeded. | Second request 409 `TourBooking.DuplicateForDate`. |
| TC-BK-013 | `POST /api/v1/booking/tour` | `CreateTourBookingCommandHandler` | Phase C #8, Concurrency | Customer | Request participants greater than slot available. | 409 `AvailabilitySlot.CapacityExceeded`; no booking row; `LockedCount` unchanged in subsequent availability GET. |
| TC-BK-014 | `POST /api/v1/booking/tour` | `CreateTourBookingCommandHandler` | Phase C private tour | Customer | Book empty slot with `IsPrivate=true`; then attempt any booking on same slot from `userB`. | Private booking 201 if supported; second request 409 capacity. If private toggle not fully enforced in code, mark divergence: handler locks participant count, not entire max capacity. |
| TC-BK-015 | `POST /api/v1/booking/tour` race | `CreateTourBookingCommandHandler` | Phase C lock + RowVersion | Customer A/B | Use `browser_tabs` to open two tabs/contexts, login `userA` and `userB`; prepare same one-seat slot; trigger two `browser_evaluate` POSTs concurrently. | Exactly one 201; loser gets 409 `AvailabilitySlot.CapacityConflict` or `CapacityExceeded`; final availability shows no negative available count; only one active `SlotLock`/booking. |
| TC-BK-016 | `GET /api/v1/booking/{id}` | `GetTourBookingByIdQuery` | Phase B/C detail | Owner/Guide/Admin | Owner GET booking; switch tab to unrelated customer; switch to admin. | Owner 200; unrelated customer 403 `TourBooking.OwnerMismatch`; admin 200; anonymous 401. |
| TC-BK-017 | `GET /api/v1/booking/my-bookings?...` | `GetMyBookingsQuery` | Phase G | Customer | Create several bookings; GET with status, fromDate, toDate, tourId, pageSize, cursor. | 200; only caller bookings; status comma parsing; invalid status -> 400 `TourBooking.InvalidStatusFilter`; invalid date -> `TourBooking.InvalidDateRange`; pageSize clamped [1,50]. |
| TC-BK-018 | `GET /api/v1/booking/admin/all?...` | `GetAllBookingsQuery` | Admin dashboard | Admin | Login admin; GET filters by status/date/user/provider/tour/paymentStatus. | 200; includes all users; customer gets 403; invalid status/date errors; `paymentStatus` is no-op hint until Finance projection ships. |
| TC-BK-019 | `POST /api/v1/booking/{id}/confirm` | `ConfirmTourBookingCommand` | Phase F | Guide/Admin | Create non-instant booking in PendingConfirmation (or force via seed); login provider guide; POST confirm. | 200; status `Confirmed`; `ConfirmationSource=Manual`; unauthorized customer 403; invalid state (Completed/Cancelled) -> 422/400 `TourBooking.InvalidState`. |
| TC-BK-020 | `POST /api/v1/booking/{id}/reject` | `RejectTourBookingCommand` | Phase F | Guide/Admin | PendingConfirmation booking; POST reason >=10. | 200; status `Rejected`; refundAmount = total/100% path; reason persisted; reason <10 -> validation; non-provider 403; non-PendingConfirmation -> invalid state. |
| TC-BK-021 | `POST /api/v1/booking/{id}/cancel` | `CancelTourBookingCommand` | Phase H free cancel | Customer | Confirmed booking with refund policy tier `hoursBeforeTour` full-refund threshold; cancel far before tour. | 200; status `Cancelled`; `CancellationSource=User`; refund equals 100% tier; slot availability restored; outbox cancellation event visible if DB checked. |
| TC-BK-022 | `POST /api/v1/booking/{id}/cancel` | `CancelTourBookingCommand` | Phase H partial/no refund | Customer | Cancel bookings at partial window and late/no-refund window. | Partial returns configured percent (e.g., 50% if seeded); late returns 0%; if snapshot missing `{}`, refund is 0% (known divergence/default-policy risk). |
| TC-BK-023 | `POST /api/v1/booking/{id}/cancel` | `CancelTourBookingCommand` | Phase H provider/admin | Guide/Admin | Provider cancels confirmed booking with reason >=10; admin force-style cancel via same endpoint if allowed. | Provider/Admin cancellation always 100% refund; reason required >=10; owner reason optional; terminal states Cancelled/Rejected/Completed cannot cancel. |
| TC-BK-024 | `POST /api/v1/admin/bookings/{id}/force-refund` | `AdminForceRefundCommand` | Phase H admin | Admin | Login admin; POST force majeure reason. | 200; status Cancelled/Refunded per result; 100% refund regardless policy; customer/guide 403; short reason validation. |
| TC-BK-025 | `POST /api/v1/booking/{id}/complete` | `CompleteTourBookingCommand` | Phase G | Guide/Admin | Confirmed booking with slot already started; POST complete. | 200; status `Completed`; `CompletedByUserId`; before slot start -> `TourBooking.NotYetStarted`; non-confirmed -> invalid state; emits completed event for Finance escrow. |
| TC-BK-026 | `POST /api/v1/booking/join-requests` | `SubmitJoinRequestCommand` | Phase I | Customer | As `userB`, submit request for existing confirmed non-private booking with remaining capacity. | 200/201; status Pending; locks requested participant count; owner cannot request own booking; private/full slot rejected. |
| TC-BK-027 | `GET /api/v1/booking/join-requests` | `GetJoinRequestsQuery` | Phase I | Customer/Guide | GET `?tourBookingId=...`; GET `?myRequestsOnly=true`. | 200; requester sees own; guide sees requests for provider bookings; unauthorized/other user denied if handler enforces ownership. |
| TC-BK-028 | `POST /api/v1/booking/join-requests/{id}/approve` | `ApproveJoinRequestCommand` | Phase I | Guide | Approve pending join request with response message. | 200; JoinRequest Approved; resulting booking created/attached if implemented; slot lock remains until joiner payment; non-guide 403; already approved/rejected invalid. |
| TC-BK-029 | `POST /api/v1/booking/join-requests/{id}/reject` | `RejectJoinRequestCommand` | Phase I | Guide | Reject pending join request. | 200; status Rejected; locked capacity released; requester can see response; non-guide 403. |
| TC-BK-030 | `GET /api/v1/booking/guide-discounts/mine` | `GetMyGuideDiscountsQuery` | Phase D guide discounts | Guide | Login guide; GET discounts. | 200; only current guide discounts; customer 403 unless permission incorrectly assigned. |
| TC-BK-031 | `POST /api/v1/booking/guide-discounts` | `CreateGuideDiscountCommand` | Phase D | Guide | Create Percentage and FixedAmount discounts for guide/tour. | 200/201; fields use actual model `GuideUserId + optional TourId`; no `MinParticipants`; invalid percentage/fixed values rejected. |
| TC-BK-032 | `PUT /api/v1/booking/guide-discounts/{id}` | `UpdateGuideDiscountCommand` | Phase D | Guide | Update dates/value/active discount. | 200; own discount only; invalid date range rejected; `MaxUsageCount` less than current usage rejected if implemented. |
| TC-BK-033 | `DELETE /api/v1/booking/guide-discounts/{id}` | `DeactivateGuideDiscountCommand` | Phase D | Guide | Delete/deactivate own discount; list again. | 200/no-content; discount inactive/not applied to future booking; second delete idempotent or invalid-state per implementation. |
| TC-BK-034 | `GET /api/v1/booking/slots/availability/{tourId}` duplicate route | `GetAvailabilityForTourQuery` | Known route duplication | Anonymous | Call both `/api/v1/booking/availability/{tourId}` and `/api/v1/booking/slots/availability/{tourId}`. | Both currently mapped because `AvailabilitySlotEndpoints` are mounted twice. Record whether duplicate route names cause startup issue; expected no duplicate name crash in current build only if ASP.NET accepts duplicates here. |
| TC-BK-035 | Background: payment completed | `PaymentCompletedHandler` | Phase E | System/Finance | Use Finance fake gateway/webhook or seed `PaymentCompletedIntegrationEvent`; wait with `browser_wait_for` / poll booking. | AwaitingPayment instant booking transitions Confirmed with `ConfirmationSource=PaymentWebhook`; non-instant transitions PendingConfirmation; idempotent duplicate event does not double-confirm. |
| TC-BK-036 | Background: AwaitingPayment expiry | `BookingAutoExpireService` | Phase E | System | Create AwaitingPayment with short/seeded expired `PaymentExpiresAt`; wait/poll. | Status becomes payment-expired path; slot lock released; `TourBookingPaymentExpiredIntegrationEvent` published. |
| TC-BK-037 | Background: provider auto-accept | `ProviderAutoAcceptService` | Phase F | System | Seed PendingConfirmation older than 24h; wait/poll service. | Status Confirmed; `ConfirmationSource=AutoAccept`; guide/tourist notification event available. |
| TC-BK-038 | Background: slot lock cleanup TTL | `SlotLockCleanupService` | Phase C lock TTL | System | Create booking/lock; set lock expired or wait 10m in test env; poll availability. | Expired lock released; `LockedCount` decremented; availability restored; no negative counts. |
| TC-BK-039 | Background: join request expiry | `JoinRequestExpiryService` | Phase I | System | Seed pending join request older than 48h; wait/poll. | Status Expired; slot lock released; requester notification event published. |
| TC-BK-040 | Background: auto-complete | `BookingAutoCompleteService` | Phase G | System | Seed confirmed booking with slot ended >2h. | Status Completed; completed event triggers Finance escrow path. |
| TC-BK-041 | Background: reminders | `BookingReminderService` | Phase G reminders | System | Seed confirmed booking T-24h/T-2h; wait/poll outbox. | `BookingReminderIntegrationEvent` published once per reminder window; no duplicates on next poll. |
| TC-BK-042 | Background: slot cleanup | `SlotCleanupService` | Phase A cleanup | System | Seed old inactive slot with no bookings/locks; wait/poll availability. | Old orphan slot removed/deactivated; slots with bookings/active locks untouched. |

## 2. NOT_BUILT — Skip skeletons

| ID | Planned item | Workflow § | Current status / skip reason | Skeleton MCP assertion |
|---|---|---|---|---|
| SKIP-BK-001 | Booking-owned `POST /bookings/{id}/pay` | Phase E | NOT_BUILT by design; payment initiation lives in Finance. | Navigate/call `/api/v1/booking/{id}/pay`; expect 404; verify Finance `/payments/initiate` separately. |
| SKIP-BK-002 | Booking-owned `POST /payments/webhook` | Phase E | NOT_BUILT; webhook is Finance-owned. | Call Booking webhook route; expect 404; Finance webhook belongs to Finance tests. |
| SKIP-BK-003 | `GET /bookings/{id}/payment-status` | Phase E | NOT_BUILT in Booking. | Route should 404 until Booking delegates to Finance or UI calls Finance directly. |
| SKIP-BK-004 | `GET /bookings/my/payment-history` | Phase E | NOT_BUILT in Booking; Finance has payment/invoice endpoints. | 404 in Booking; cover in Finance suite. |
| SKIP-BK-005 | Razor UI `YallaJo.Web/Areas/Booking` | UI context | Path missing in current checkout. | `browser_navigate("https://localhost:57065/swagger/Booking")` may 404; use Swagger/API fallback. |
| SKIP-BK-006 | Check-in participant endpoint | Phase G dashboard | `CheckInBookingCommand` missing. | Planned route `/guides/me/bookings/{id}/check-in` should 404. |
| SKIP-BK-007 | Mark no-show endpoint | Phase G dashboard | `MarkNoShowCommand` missing. | Planned route `/guides/me/bookings/{id}/no-show` should 404. |
| SKIP-BK-008 | Guide booking dashboard routes | Phase 7 | `ListGuideBookingsQuery`, upcoming/detail dashboard queries missing. | Planned `/guides/me/bookings*` should 404 unless added later. |
| SKIP-BK-009 | Real discount evaluator | Phase D | `NoOpDiscountEvaluator` still registered in services list; discounts CRUD exists but booking pricing may not apply them. | Create valid discount then book; expect discount remains 0 until evaluator replaced. |
| SKIP-BK-010 | Real commission lookup | Phase C pricing | `StubBookingCommissionLookup` returns flat 10%. | Assert commissionRate = 0.10; mark as stub, not business-final. |
| SKIP-BK-011 | Real cross-module snapshot readers if stubs registered | Phase C | Current services include both real and stub/throwing classes; verify DI actual registration before asserting full cross-module truth. | If stub active, fake tour/provider data may pass invalid IDs. |
| SKIP-BK-012 | Loyalty redemption | Phase C/D | Handler stamps loyalty amount 0; Finance sprint deferred. | Booking with `LoyaltyPointsToRedeem>0` should not reduce total; document deferred. |
| SKIP-BK-013 | Subscriber 15-minute slot lock TTL | Decision #7 | Handler hardcodes 10 min `SlotLockTtl`; no subscriber branch. | Subscriber user still gets 10m; mark NOT_BUILT. |
| SKIP-BK-014 | Private locks entire max capacity | Private Tour Details | Handler currently calls `slot.Lock(participantCount)` regardless `IsPrivate`; no guide private capability check observed in handler. | If private slot not fully reserved, bug/divergence. |
| SKIP-BK-015 | Confirmation code exact `YJ-YYYYMM-XXXX` | User output requirement | Plan/code mention `YJ-YYYYMMDD-XXXXXX`; generator must be inspected before requiring shorter format. | Assert accepted formats and flag divergence. |
| SKIP-BK-016 | BusinessReservation flow | Future Extension Blueprint | Explicit future extension. | No active endpoints expected. |

## 3. DEFERRED — Post-MVP

| ID | Deferred item | Source | Test treatment |
|---|---|---|---|
| DEF-BK-001 | Multi-tour package booking / PackageBooking | Booking-Workflow Decision #11; `_Deferred/` | No Playwright test except route absence. |
| DEF-BK-002 | BusinessReservation / BusinessAvailabilitySlot / BusinessRefundPolicy | Booking-Workflow future blueprint | Separate future module suite. |
| DEF-BK-003 | Loyalty redemption | Handler comment: Finance sprint | Keep negative assertion until Finance implements. |
| DEF-BK-004 | Referral/promo-code stack integration beyond no-op evaluator | Discount pipeline | Test skeleton only; active booking discount stays zero until evaluator real. |
| DEF-BK-005 | Duplicate Booking.Domain `TourGuide` removal / enum cleanup | FixPlan deferred | No browser scenario; migration/refactor task. |

## 4. Integration Events (Publishes / Consumes)

### Publishes from Booking

- `TourBookingCreatedIntegrationEvent` (`tour-booking.created.v1`) — TC-BK-007, TC-BK-015.
- `TourBookingConfirmedIntegrationEvent` — TC-BK-019, TC-BK-035, TC-BK-037.
- `TourBookingRejectedIntegrationEvent` — TC-BK-020.
- `TourBookingCancelledIntegrationEvent` — TC-BK-021..024, suspension cascade.
- `TourBookingCompletedIntegrationEvent` — TC-BK-025, TC-BK-040; Finance escrow should consume.
- `TourBookingPaymentExpiredIntegrationEvent` — TC-BK-036.
- `JoinRequestCreatedIntegrationEvent`, `JoinRequestApprovedIntegrationEvent`, `JoinRequestRejectedIntegrationEvent` — TC-BK-026..029.
- `SlotLockCreatedIntegrationEvent`, `SlotLockReleasedIntegrationEvent` — TC-BK-007, TC-BK-015, TC-BK-038/039.
- `AvailabilitySlotCapacityChangedIntegrationEvent` — TC-BK-003..006, TC-BK-015.
- `BookingReminderIntegrationEvent` — TC-BK-041.

### Consumes into Booking

- Finance `PaymentCompletedIntegrationEvent` → `PaymentCompletedHandler` (TC-BK-035).
- Accounts `ProviderSuspendedIntegrationEvent` → provider future booking cancellation.
- ContentTours `TourGuideSuspendedIntegrationEvent` → guide future booking cancellation.
- Current code also contains handlers for guide offering suspended, tour suspended/deleted/updated/pricing changed/approved snapshots; test with seeded integration events when event publishers are wired.
- Finance refund processed/completed is Finance-owned; Booking cancellation stores refund amount and publishes events for Finance to process.

## 5. Validation Matrix

| Area | Rules to assert | Expected status/code |
|---|---|---|
| Create booking shape | Required TourId, AvailabilitySlotId, ParticipantBreakdown; adult >=1; non-negative child/infant/senior; total <=100 | 400 validation |
| Promo code | <=20 chars, uppercase A-Z/0-9 only | 400 validation |
| Special requests | <=2000 chars | 400 validation |
| Slot existence/activity | active slot matching tour | 404 `AvailabilitySlot.NotFound` |
| Provider/tour status | active approved tour, provider not suspended | 404 `Tour.NotFound` / 409 `Booking.ProviderSuspended` |
| Lead time | slot start at least 2h away | invalid `TourBooking.TooEarly` |
| Pending cap | <3 AwaitingPayment per user | 409 `TourBooking.ConcurrentLimit` |
| Duplicate date | no same user+tour+date active booking | 409 `TourBooking.DuplicateForDate` |
| Capacity | `BookedCount + LockedCount + requested <= MaxCapacity` | 409 `AvailabilitySlot.CapacityExceeded` |
| Currency | JOD/USD/EUR only | invalid `TourBooking.UnsupportedCurrency` |
| Minimum total | JOD total >= 5 | invalid `TourBooking.BelowPlatformMinimum` |
| Provider/admin cancellation reason | required, >=10 chars | invalid `TourBooking.CancellationReasonRequired` |
| Reject reason | required, 10-500 chars per endpoint summary/validator | validation |
| RowVersion | stale availability update/delete | 409 concurrency |

## 6. Auth Matrix (Anonymous | Customer | Guide | Admin)

| Endpoint group | Anonymous | Customer | Guide/provider | Admin |
|---|---:|---:|---:|---:|
| Availability GET | ✅ | ✅ | ✅ | ✅ |
| Availability create/update/delete/bulk | ❌ 401 | ❌/403 | ✅ if permission | ✅ |
| Create tour booking | ❌ 401 | ✅ | ⚠️ only if customer permission granted | ✅ if permission, but should not bypass business rules |
| Get booking by id | ❌ 401 | ✅ owner only | ✅ provider only | ✅ |
| My bookings | ❌ 401 | ✅ own | ✅ own if any | ✅ own/admin should use admin all |
| Admin all / force refund | ❌ 401 | ❌ 403 | ❌ 403 unless elevated | ✅ |
| Confirm/reject/complete | ❌ 401 | ❌ 403 | ✅ provider/guide | ✅ |
| Cancel | ❌ 401 | ✅ own | ✅ provider with reason | ✅ with reason |
| Join request create | ❌ 401 | ✅ | ✅ as requester if allowed | ✅ |
| Join request approve/reject | ❌ 401 | ❌ 403 | ✅ provider/guide | ✅ if permission |
| Guide discounts | ❌ 401 | ❌ 403 | ✅ own | ✅ depending permission |

## 7. State Machine Transitions (AwaitingPayment → Confirmed → Completed → Disputed; Cancelled by user/provider/admin)

| From | Trigger | To | Scenario |
|---|---|---|---|
| New | `CreateTourBookingCommand` | `AwaitingPayment` | TC-BK-007 |
| AwaitingPayment instant | Finance `PaymentCompletedIntegrationEvent` | `Confirmed` | TC-BK-035 |
| AwaitingPayment non-instant | Finance `PaymentCompletedIntegrationEvent` | `PendingConfirmation` | TC-BK-035 |
| AwaitingPayment/PendingConfirmation | `ConfirmTourBookingCommand` | `Confirmed` | TC-BK-019 |
| PendingConfirmation | `RejectTourBookingCommand` | `Rejected` | TC-BK-020 |
| PendingConfirmation older than 24h | `ProviderAutoAcceptService` | `Confirmed` | TC-BK-037 |
| Confirmed after slot start | `CompleteTourBookingCommand` | `Completed` | TC-BK-025 |
| Confirmed after slot end + 2h | `BookingAutoCompleteService` | `Completed` | TC-BK-040 |
| AwaitingPayment expired | `BookingAutoExpireService` | payment-expired state/path | TC-BK-036 |
| AwaitingPayment/PendingConfirmation/Confirmed | user cancel | `Cancelled` with policy refund | TC-BK-021/022 |
| PendingConfirmation/Confirmed | provider cancel | `Cancelled` with 100% refund | TC-BK-023 |
| Any active | admin force refund | `Cancelled` with 100% refund | TC-BK-024 |
| Completed | dispute | Finance/Social dispute flow, not Booking-built | NOT_BUILT for Booking; Finance dispute suite. |

## 8. Concurrency Scenarios

1. **Slot lock TTL 10 min** — TC-BK-038: create AwaitingPayment booking, verify `LockedCount` increases, wait/seed expiry, verify lock release and booking payment-expired. Subscriber 15m branch is NOT_BUILT.
2. **Multiple users racing same slot** — TC-BK-015: use `browser_tabs` to login userA/userB in separate tabs/contexts and issue simultaneous `fetch` POSTs. Assert exactly one success.
3. **Optimistic concurrency RowVersion conflict** — TC-BK-005/006: two tabs load same slot rowVersion; tab A updates/deletes; tab B submits stale rowVersion; assert 409.
4. **3 concurrent pending bookings per user limit** — TC-BK-011: create 3 AwaitingPayment bookings for userA, fourth returns 409.
5. **2h minimum lead time enforcement** — TC-BK-010: slot within 2h returns invalid.
6. **Capacity overflow** — TC-BK-013: participant count > available returns conflict; race loser returns capacity conflict; availability never below zero.
7. **Duplicate booking conflict** — TC-BK-012: same user, tour, date rejected.

## 9. Confirmation Code Generation (YJ-YYYYMM-XXXX format uniqueness)

- Requirement says assert `YJ-YYYYMM-XXXX` uniqueness.
- Booking workflow/code comments say `YJ-YYYYMMDD-XXXXXX`; active `CreateTourBookingResult.Reference` should be captured for every create scenario.
- Playwright assertion:
  1. Create 20 bookings across users/slots using `browser_evaluate`.
  2. Assert all references unique.
  3. Accept current regex `^YJ-\d{8}-[A-Z0-9]{6}$` if generator follows code plan; flag divergence if product insists on `^YJ-\d{6}-[A-Z0-9]{4}$`.
  4. Verify reference remains immutable after confirm/cancel/complete.

## 10. Instant vs Non-Instant Booking (24h provider confirm window)

- **Instant**: create booking on instant tour → Finance payment completed event → booking should become `Confirmed` automatically with `ConfirmationSource=PaymentWebhook`.
- **Non-instant**: create booking on non-instant tour → payment completed event → booking should become `PendingConfirmation`; provider can confirm/reject; no response after 24h triggers `ProviderAutoAcceptService` and sets `ConfirmationSource=AutoAccept`.
- **Manual confirm caveat**: current `ConfirmTourBookingCommandHandler` allows confirming `AwaitingPayment` or `PendingConfirmation`. Test should verify whether confirming AwaitingPayment before payment is intentional; if it confirms unpaid bookings, log as security/business divergence.

## 11. Known Divergence (per CrossDocumentAnalysisReport.md)

- Refund policy contradiction was resolved toward Booking's 3-tier per-tour policy, but Finance/default policy still needs definitive product/legal confirmation. Tests must use explicit `RefundPolicySnapshot` in seed data and not assume global defaults.
- Provider suspension cascade is only partially resolved cross-document. Booking has handlers for some suspension/deletion events, but upstream ContentTours/Accounts event publication and Social/Finance side effects must be validated in integration suites.
- Booking plan originally placed payment gateway/webhook in Booking. Current architecture correctly keeps `IPaymentGateway` and webhooks in Finance; Booking consumes Finance `PaymentCompletedIntegrationEvent`.
- Audit said bulk slot creation, reminder, slot cleanup, and guide offering suspended handlers were missing; current code contains these, so tests should treat them as built but still verify route wiring and background-service behavior.
- GuideDiscount model diverges from plan: actual uses `GuideUserId` + optional `TourId`, lacks `MinParticipants`, and names flat discounts `FixedAmount`.
- Private tour plan requires locking entire `MaxCapacity`; current create handler appears to lock only requested participant count. TC-BK-014 should expose this if still true at runtime.
- UI route divergence: reference says Web UI under `YallaJo.Web/Areas/Booking`, but that path is absent in this checkout; active Playwright tests should run via API/Swagger until UI exists.
