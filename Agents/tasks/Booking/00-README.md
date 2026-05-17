# Booking Module — Wave 5 Sprint

> **Predecessor sprint:** [`../ContentBlogs-ContentSeo-team-tasks.md`](../ContentBlogs-ContentSeo-team-tasks.md) closes 2026-06-12
> **This sprint covers:** complete `Booking` bounded-context module — first half of Wave 5 (booking engine, availability, refund policies, provider documents)
> **Difficulty vs ContentTours:** ⚙️⚙️⚙️⚙️⚙️ (5/5) — the hardest sprint in the project. Optimistic concurrency, 5-step state machine, escrow handoff to Finance, 4 background services.
> **Endpoint count:** **22 HTTP endpoints**
> **Background services:** 4 hosted services
> **Working-day estimate:** **40 working days × 4 devs ≈ 190 person-hours**

---

## 0. Sprint Window & Hard Deadlines

Working week is **Sun → Thu** (5 days). All times AST (UTC+3).

| Milestone | Date | Time | Notes |
|---|---|---|---|
| Pre-work cut | Fri **2026-06-12** | 17:00 | Tech Lead branches `sprint/booking-prework` from `main` |
| Sprint kickoff | Sun **2026-06-14** | 09:00 | Architecture walkthrough (90 min, all devs mandatory) |
| Pre-work merge deadline | Tue **2026-06-16** | 17:00 | All PW-* items merged to `main` |
| Earliest task start | Wed **2026-06-17** | 09:00 | Feature work begins |
| Mid-sprint integration freeze | Sun **2026-07-19** | 17:00 | All BG services + state machine merged; only bug-fix PRs after this |
| Hard PR cutoff | Wed **2026-08-12** | 17:00 | No new feature PRs accepted |
| Hard merge-to-main cutoff | Thu **2026-08-13** | 17:00 | `v1.5.0-booking-complete` tag cut |
| Sprint retro + Finance kickoff | Fri **2026-08-14** | 11:00 | Retro 60 min, Finance kickoff 30 min |

Total: **8 calendar weeks (40 working days)**.

### 0.1 Daily Standup

Every Sun → Thu at **09:30 AST**. 15-minute hard cap. Each dev gives 3 sentences: yesterday / today / blockers. Two consecutive misses → escalate to Tech Lead. Skip standup only on public holidays or pre-arranged PTO.

---

## 1. Working Days & Person-Hour Budget

| Bucket | Value |
|---|---|
| Working days | 40 |
| Hours per day per dev | 6 (after standups + reviews + buffer) |
| Devs | 4 |
| **Total available hours** | **960** |
| Feature task hours | 540 (56%) |
| Code review hours | 120 (12%) |
| Ceremony hours (standups, retro, kickoff, demo) | 80 (8%) |
| Pre-work hours (Tech Lead) | 40 (4%) |
| Buffer / unplanned (bugs, doc updates, env issues) | 180 (19%) |

This sprint is intentionally over-budgeted on buffer (19%) because **the 5-step `POST /tour` engine carries the highest failure-mode risk in the entire project**. Concurrency bugs caught late explode.

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG Services | Est Hours | Hard Deadline |
|---|---|---|---|---|---|---|
| **Mohammad** (lead) | Intermediate | TASK 4 (Booking Engine) + TASK 5 (Confirm/Reject/Cancel) | 9 | 0 | 80 | 2026-08-12 |
| **Mahmoud** | Intermediate | TASK 1 (Availability Slots) + TASK 7 (BG Services) | 5 | 4 | 60 | 2026-08-12 |
| **Fadwa** | Beginner | TASK 2 (Refund Policy + Commission) + TASK 3 (Provider Documents) + TASK 6 (Join Request) | 8 | 0 | 50 | 2026-08-12 |
| **Tech Lead** | Senior | Pre-Work (PW-1..PW-8) + code review + integration test harness | 0 | 0 | 40 | 2026-06-16 (PW) / 2026-08-13 (review) |

---

## 3. Deliverable Manifest (table of contents)

| File | Section | Owner | Status |
|---|---|---|---|
| [`01-pre-work.md`](./01-pre-work.md) | Pre-Work PW-1..PW-8 (Tech Lead) | Tech Lead | Pending |
| [`02-critical-rules.md`](./02-critical-rules.md) | Booking-specific rules (additive to INDEX §4) | Tech Lead | Pending |
| [`03-entities-matrix.md`](./03-entities-matrix.md) | Entity ownership matrix for all 11 Booking entities | Tech Lead | Pending |
| [`04-task-availability-slots.md`](./04-task-availability-slots.md) | TASK 1 — AvailabilitySlot CRUD + bulk-recurring 90-day | Mahmoud | Pending |
| [`05-task-refund-policy-commission.md`](./05-task-refund-policy-commission.md) | TASK 2 — RefundPolicy CRUD + CommissionRule CRUD | Fadwa | Pending |
| [`06-task-provider-documents.md`](./06-task-provider-documents.md) | TASK 3 — Provider documents upload + expiry tracking | Fadwa | Pending |
| [`07-task-booking-engine.md`](./07-task-booking-engine.md) | TASK 4 — **The 5-step `POST /tour` booking engine** | Mohammad | Pending |
| [`08-task-confirm-reject-cancel.md`](./08-task-confirm-reject-cancel.md) | TASK 5 — Provider confirm/reject, user cancel-with-refund | Mohammad | Pending |
| [`09-task-join-request.md`](./09-task-join-request.md) | TASK 6 — Join-request workflow (request, approve, reject) | Fadwa | Pending |
| [`10-task-background-services.md`](./10-task-background-services.md) | TASK 7 — 4 background services (SlotLockCleanup, BookingAutoExpire, ProviderAutoAccept, DocumentExpiryCheck) | Mahmoud | Pending |
| [`11-cross-cutting.md`](./11-cross-cutting.md) | DI audit, permission seeder, outbox registry, migrations, build lock | Tech Lead | Pending |
| [`99-acceptance-gate.md`](./99-acceptance-gate.md) | Final acceptance gate sign-off checklist | Tech Lead | Pending |

---

## 4. Endpoint Count Verification

22 endpoints distributed across tasks:

| Task | Endpoints | Routes |
|---|---|---|
| TASK 1 (Availability Slots) | 5 | `GET /availability/{tourId}`, `GET /availability/{tourId}/{date}`, `POST /availability/slots`, `PUT /availability/slots/{id}`, `DELETE /availability/slots/{id}`, **`POST /availability/slots/bulk`** (recurring 90-day) |
| TASK 2 (Refund Policy + Commission) | 6 | `GET /refund-policies/{tourId}`, `POST /refund-policies`, `PUT /refund-policies/{id}`, `GET /commissions`, `POST /commissions`, `PUT /commissions/{id}`, `DELETE /commissions/{id}` |
| TASK 3 (Provider Documents) | 4 | `GET /provider/documents`, `POST /provider/documents`, `PUT /provider/documents/{id}`, `DELETE /provider/documents/{id}` |
| TASK 4 (Booking Engine) | 4 | `POST /tour` (5-step engine), `GET /{id}`, `GET /my-bookings`, `GET /admin/all` |
| TASK 5 (Confirm/Reject/Cancel) | 5 | `POST /{id}/confirm`, `POST /{id}/reject`, `POST /{id}/cancel`, `POST /{id}/complete`, `GET /provider/{pending,upcoming,history}` (3 routes counted as 1 endpoint group with query-param) |
| TASK 6 (Join Request) | 3 | `POST /join-request`, `POST /join-request/{id}/approve`, `POST /join-request/{id}/reject` |
| TASK 7 (BG Services) | 0 (background only) | n/a |
| **Total** | **22** | (counting `POST /availability/slots/bulk` as separate from CRUD pair makes 22; bulk is sufficiently distinct in implementation cost) |

---

## 5. Integration Events Emitted (consumed by downstream Finance / Messaging / Analytics)

Every event follows logical name convention `booking.{entity}.{verb}.v1` and is registered in `IntegrationEventTypeRegistry`.

| Event Name | Trigger | Payload (key fields) |
|---|---|---|
| `booking.tour-booking.created.v1` | `POST /tour` after Step 4 (booking row inserted with `AwaitingPayment`) | bookingId, tourId, userId, slotId, currency, totalAmount, participantCount, providerId |
| `booking.tour-booking.confirmed.v1` | `POST /payments/webhook` (Finance) flips booking AwaitingPayment→Confirmed instant; or provider confirm 24h non-instant | bookingId, confirmedAt, tourId, userId |
| `booking.tour-booking.cancelled.v1` | `POST /{id}/cancel` (user or provider initiated) | bookingId, cancelledBy (User/Provider/System), reason, refundAmount, refundCurrency |
| `booking.tour-booking.completed.v1` | `POST /{id}/complete` after tour date passes | bookingId, tourId, userId, providerId, completedAt |
| `booking.tour-booking.rejected.v1` | Provider `POST /{id}/reject` for non-instant booking within 24h window | bookingId, rejectionReason, providerId |
| `booking.slot-lock.expired.v1` | SlotLockCleanupService (5-min job) deletes expired lock | bookingId, slotId, lockExpiredAt |
| `booking.provider-document.expiring.v1` | DocumentExpiryCheckService (daily) detects doc <30 days from expiry | providerId, documentId, documentType, expiresAt |
| `booking.provider-document.expired.v1` | DocumentExpiryCheckService when ExpiresAt < Now | providerId, documentId, documentType |
| `booking.provider.suspended-doc-expired.v1` | DocumentExpiryCheckService 14-day grace exceeded → provider auto-suspended | providerId, expiredDocumentIds |
| `booking.join-request.approved.v1` | TASK 6 approve | joinRequestId, bookingId, additionalParticipantUserId |
| `booking.join-request.rejected.v1` | TASK 6 reject | joinRequestId, bookingId, reason |

---

## 6. Downstream Consumer Map (for testing acceptance gates)

| Event | Consumer Module | Handler | Expected Effect |
|---|---|---|---|
| `tour-booking.created` | Finance | `BookingCreatedInboxHandler` | Pre-creates `Payment` row (`PaymentStatus.Pending`) so webhook has FK target |
| `tour-booking.confirmed` | Messaging | `BookingConfirmedInboxHandler` | Notification `BookingConfirmed` to user + provider |
| `tour-booking.confirmed` | Analytics | `BookingConfirmedInboxHandler` | UserInteraction `BookingCompleted` + PopularityScore increment |
| `tour-booking.cancelled` | Finance | `BookingCancelledInboxHandler` | If refundAmount>0, initiate refund via `IPaymentGateway` |
| `tour-booking.cancelled` | Messaging | `BookingCancelledInboxHandler` | Notification `BookingCancelled` to opposite party |
| `tour-booking.completed` | Social | `BookingCompletedInboxHandler` | Allow review submission window opens (30-day) |
| `tour-booking.completed` | Finance | `BookingCompletedInboxHandler` | Mark booking eligible for next weekly payout batch |
| `provider-document.expiring` | Messaging | `DocumentExpiringInboxHandler` | Notification `DocumentExpiryWarning` to provider |
| `join-request.approved` | Finance | `JoinRequestApprovedInboxHandler` | Charge additional participant separately via gateway |

---

## 7. Reading Order

1. [`../Phase1-Phase2-Completion-INDEX.md`](../Phase1-Phase2-Completion-INDEX.md) §4 — universal critical rules.
2. This README.
3. [`01-pre-work.md`](./01-pre-work.md) — required reading before any task starts.
4. [`02-critical-rules.md`](./02-critical-rules.md) — module-specific delta.
5. [`03-entities-matrix.md`](./03-entities-matrix.md) — to find which entities you touch.
6. Your assigned `0N-task-*.md` file(s).
7. [`99-acceptance-gate.md`](./99-acceptance-gate.md) — what "done" looks like.

---

## 8. Out-of-Scope (Backlog for Future Sprints)

- **Package booking** (`PackageBooking` entity exists but is Phase 3 / Wave 5 second-half — deferred to a future sprint after `Finance/Subscriptions` lands).
- **`Reservation` entity** (non-tour business reservation flow — defer to Wave 5 second-half with PackageBooking).
- **TourGuide assignment & language/specialization junctions** (entities exist; CRUD endpoints belong to Wave 6 ContentTours follow-up).
- Discount integration in `POST /tour` Step 3 — Finance discount engine is Phase 4; for now Step 3 uses pricing tiers + loyalty points only. Discount calculation injected as `IDiscountEvaluator` stub returning `DiscountResult.None` until Finance Wave 5 sprint #2.
