# Playwright MCP Test Scenarios — Finance Module

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans / Module score (7.0→10.0)

Sources read in full:

- `Agents/Plans/Finance-Workflow.md`
- `Agents/Plans/Finance-Audit-Report.md`
- `Agents/Plans/Finance-FixPlan.md`

References used:

- `Agents/Plans/Master-RoadmapTo10.md` — Finance is HIGH priority, score target `7.0 → 10.0`.
- `Agents/Plans/CrossDocumentAnalysisReport.md` — commission contradiction resolved to **revenue-tier**; subscription-tier commission from the PDF is deferred and must not be asserted in Built tests.
- Current code surface: `Finance.Presentation` exposes `/api/v1/payments`, `/api/v1/invoices`, `/api/v1/payouts`, `/api/v1/commissions`, `/api/v1/provider-payment-methods`, `/api/v1/disputes`, `/api/v1/finance`.

Execution note: prefer real UI at `https://localhost:57065/swagger` when available (`YallaJo.Web/Areas/Finance` or Admin Finance pages). If no Finance UI exists, use `mcp__playwright__browser_evaluate` against `https://localhost:57065/api/v1/*` with authenticated fetch calls. Use `mcp__playwright__browser_network_requests` to capture webhook/API traffic and confirm single side effects.

## 0. Prerequisites (payment gateway test mode, currency rates seeded, commission rules in DB, escrow account)

1. App startup blocker is resolved before execution: fix SQL connection bug where `Server=MOHAMMAD\SQLEXPRESS\\SQLEXPRESS` double-suffix prevents startup.
2. API runs at `https://localhost:57065`; Web runs at `https://localhost:57065/swagger`; Swagger reachable at `/swagger`.
3. Recaptcha remains globally disabled for test users.
4. Seed users exist and can log in with `TestPass!23`:
   - `admin@yallajo.test` — Administrator, financial-admin permissions.
   - `userA@yallajo.test`, `userB@yallajo.test` — customers with payment methods on file.
   - `guide-approved@yallajo.test` — Tour Guide with active `ProviderPaymentMethod`.
   - `business@yallajo.test` — Business Owner.
   - `agency@yallajo.test` — Tour Agency; test agency booking should split into two payouts: agency + guide.
5. Payment gateway is test mode only: Stripe test keys, HyperPay sandbox, or current deterministic `FakePaymentGateway`.
6. Commission rules seeded for revenue-tier model, not subscription-tier model:
   - `JOD`, `0.0000–10000.0000`, `15%`.
   - `JOD`, `10000.0000–50000.0000`, `12%`.
   - `JOD`, `50000.0000+`, `10%`.
7. Currencies/rates seeded for `JOD`, `USD`, `EUR`; values assert `decimal(19,4)` precision.
8. Escrow configuration seeded:
   - Escrow account exists.
   - Escrow hold is 7 days after tour completion.
   - Weekly payout batch target is Sunday midnight UTC.
   - Minimum payout threshold: assert configured threshold; user task expects **10 JOD** for Built tests, while plan notes guide threshold may be 20 JOD. Treat threshold mismatch as a blocker if runtime config differs.
9. Test data exists or can be created through setup API/DB seed:
   - Completed booking with paid payment for `userA` and independent guide.
   - Completed booking with paid payment for `userA` and agency provider + assigned guide.
   - Cancelled/refundable booking cases for refund-tier tests.
   - Payment/booking older than dispute window (`>7 days`) for negative dispute test.
10. Playwright MCP context helpers are available:
    - `browser_evaluate` for API calls where UI is missing.
    - `browser_network_requests` for webhook request/response capture.
    - Auth helper that logs in through UI or API and stores bearer/cookie session per role.

Recommended `browser_evaluate` helper shape:

```js
async ({ apiUrl, token, path, method = 'GET', body }) => {
  const res = await fetch(`${apiUrl}${path}`, {
    method,
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {})
    },
    body: body ? JSON.stringify(body) : undefined
  });
  const text = await res.text();
  return { status: res.status, headers: Object.fromEntries(res.headers), body: text ? JSON.parse(text) : null };
}
```

## 1. Built — Active Scenarios

### Payment creation (intent → confirm)

**F-001 — Customer creates payment intent for existing booking**

- Actor: `userA@yallajo.test`.
- Route/API: `POST /api/v1/payments/initiate`.
- Steps:
  1. Log in as userA.
  2. Create or select a pending booking/payment expectation owned by userA.
  3. Initiate payment with booking id, amount, currency, and test payment method.
  4. Capture response and network request.
- Expected:
  - `200 OK` with payment id, `GatewayPaymentId`, status `Pending`/gateway-confirmable state.
  - Amount/currency match booking-time locked values.
  - Response displays/returns 4-decimal precision where applicable.
  - Non-owner `userB` cannot initiate payment for userA's booking (`403` or ownership failure).

**F-002 — Repeated payment initiation is idempotent for same booking**

- Actor: `userA`.
- Route/API: `POST /api/v1/payments/initiate` twice with identical booking id.
- Expected:
  - Second call returns existing in-flight/paid payment, not a duplicate row.
  - `GET /api/v1/payments/my-payments` shows one payment for the booking.
  - Admin `GET /api/v1/payments/admin/all?userId={userA}` also shows one payment.

**F-003 — Gateway success webhook confirms payment**

- Actor: gateway webhook (anonymous, signed).
- Route/API: `POST /api/v1/payments/webhook`.
- Steps:
  1. Use payment from F-001.
  2. Send signed webhook envelope with `eventId`, `eventType=payment.succeeded`, `gatewayPaymentId`, `amount`, `currency`, `occurredAt`.
  3. Query `GET /api/v1/payments/{id}` as userA and admin.
- Expected:
  - Webhook returns `200 OK`, `Idempotent=false` on first receipt.
  - Payment state becomes `Confirmed`/completed equivalent.
  - Payment state machine path observed: `Pending → Confirmed`.
  - Invoice generation is eventually visible under `/api/v1/invoices/my-invoices`.

**F-004 — Gateway failure webhook marks payment failed without invoice**

- Actor: gateway webhook.
- Steps:
  1. Initiate a second pending payment.
  2. Send signed `payment.failed` envelope with failure code/message.
  3. Query payment and invoice lists.
- Expected:
  - Payment status is failed.
  - Failure code/message are visible in API/admin view.
  - No invoice is generated.
  - Retrying initiation may create/return a valid replacement according to domain rules.

### Payment webhook idempotency by TransactionId

**F-005 — Duplicate webhook replay by same EventId has single side effect**

- Actor: gateway webhook.
- Steps:
  1. Send a valid success webhook for pending payment.
  2. Replay exact same POST body and signature.
  3. Use `browser_network_requests` to capture both responses.
  4. Query payment, invoices, and admin payments.
- Expected:
  - First response: `Idempotent=false`.
  - Replay response: `Idempotent=true` or equivalent no-op marker.
  - One payment status transition only.
  - One invoice only.
  - One inbox/idempotency side-effect only; no duplicate outbox-visible integration behavior.

**F-006 — Duplicate webhook replay by same TransactionId/GatewayPaymentId but different EventId does not duplicate payment completion**

- Actor: gateway webhook.
- Steps:
  1. Complete payment using `EventId=A`, `GatewayPaymentId=G`.
  2. Send another valid success webhook with `EventId=B`, same `GatewayPaymentId=G`, same transaction details.
  3. Query `GET /api/v1/payments/admin/all` and invoice list.
- Expected:
  - No duplicate payment row because `GatewayTransactionId` is unique where not null.
  - No duplicate invoice for same booking/payment.
  - Response is success/no-op or conflict handled cleanly, never `500`.

**F-007 — Invalid webhook signature is rejected before parsing**

- Actor: anonymous.
- Route/API: `POST /api/v1/payments/webhook`.
- Steps: send malformed or unsigned body with payment-looking JSON.
- Expected:
  - `400 Bad Request`/invalid outcome with `Payment.WebhookSignatureMismatch`.
  - No payment state changes.
  - No invoice/outbox side effect.

**F-008 — Unknown GatewayPaymentId retries cleanly**

- Actor: gateway webhook.
- Steps: signed webhook with unknown `GatewayPaymentId`.
- Expected:
  - No `500`.
  - Response indicates not found/retry-later semantics.
  - No inbox message marked processed if the handler is designed to retry unknown payments.

### Payout calculation (revenue-tier commission)

**F-009 — Commission lookup uses revenue tier, not subscription tier**

- Actor: admin.
- Routes/API: `GET /api/v1/commissions`, `POST /api/v1/payouts/admin/trigger`.
- Steps:
  1. Seed provider monthly revenue below 10K JOD.
  2. Trigger payout calculation for eligible completed booking.
  3. Inspect payout/item via admin pending/detail endpoints.
- Expected:
  - Platform commission is 15% of gross.
  - No subscription tier, plan name, or subscription discount affects commission.
  - `CommissionRuleSnapshotId`/rule details reflect revenue-tier rule where exposed.

**F-010 — Mid-tier revenue uses 12% commission**

- Actor: admin.
- Steps: seed provider monthly revenue between 10K and 50K JOD, trigger payout.
- Expected: commission amount equals `gross * 0.1200`, net equals `gross - commission`, rounded/serialized with `decimal(19,4)` precision.

**F-011 — High-tier revenue uses 10% commission**

- Actor: admin.
- Steps: seed provider monthly revenue above 50K JOD, trigger payout.
- Expected: commission amount equals `gross * 0.1000`.

**F-012 — Missing commission rule falls back to configured default**

- Actor: admin.
- Steps: temporarily use currency/tier with no active rule or deactivate matching rule; trigger calculation.
- Expected:
  - Fallback rate is applied (plan notes 15%/JOD fallback).
  - Admin-visible result clearly shows no matching active rule if exposed.
  - No `500` from null rule.

### Payout disbursement (weekly Sunday batch, min payout 10 JOD)

**F-013 — Sunday batch creates payout for eligible escrow-released booking**

- Actor: admin/background equivalent.
- Route/API: `POST /api/v1/payouts/admin/trigger`.
- Setup: completed payment with `EscrowReleaseEligibleAt <= now`, provider has verified default `ProviderPaymentMethod`.
- Expected:
  - A payout batch appears under `GET /api/v1/payouts/admin/pending`.
  - Status is `Pending` or `Processing` according to current implementation.
  - Batch groups by provider/recipient and currency.
  - Amount meets minimum payout threshold.

**F-014 — Payout below 10 JOD remains pending/not batched**

- Actor: admin.
- Setup: eligible net payout amount `< 10.0000 JOD`.
- Expected:
  - Trigger skips payout or keeps earnings/payment pending until threshold is met.
  - Admin pending payout list does not show a disbursement below threshold.
  - No gateway payout call occurs.

**F-015 — Non-Sunday automatic batch does not run early**

- Actor: background service validation via test clock/logs where available.
- Steps: set/test current time to non-Sunday; observe no automatic sweep until target time.
- Expected:
  - Manual trigger remains possible.
  - Background schedule target is Sunday midnight UTC.

**F-016 — Admin approves large pending payout**

- Actor: admin.
- Route/API: `POST /api/v1/payouts/{id}/approve`.
- Expected:
  - `200 OK` with updated payout status.
  - Non-admin/provider receives `403` for same route.
  - Payout detail shows approved-by admin where exposed.

**F-017 — Provider can view only own payouts**

- Actor: `guide-approved`/business provider and `userB`.
- Route/API: `GET /api/v1/payouts/provider`, `GET /api/v1/payouts/{id}`.
- Expected:
  - Provider with `provider_id` claim sees own payout list.
  - Customer/no provider claim gets `403`/`Payout.OwnerMismatch`.
  - Another provider cannot view target payout detail.
  - Admin can view all.

### Invoice generation

**F-018 — Payment completion auto-generates invoice**

- Actor: userA + gateway.
- Routes/API: `/payments/webhook`, `/invoices/my-invoices`, `/invoices/{id}`.
- Expected:
  - Invoice is generated after payment confirmed.
  - Invoice includes buyer, provider, booking/payment ids, gross/net/commission/tax fields where exposed.
  - Currency equals locked booking currency.
  - Amounts render with four-decimal precision in API and DOM.

**F-019 — Invoice generation is idempotent per booking/payment**

- Actor: gateway webhook replay.
- Steps: replay completed webhook and query invoice list.
- Expected: exactly one invoice exists for the payment/booking.

**F-020 — Customer downloads own invoice PDF**

- Actor: userA.
- Route/API: `GET /api/v1/invoices/{id}/download`.
- Expected:
  - `200 OK` with `application/pdf`.
  - Filename is stable and invoice-specific.
  - First lazy render may persist PDF metadata; second download returns same invoice, not duplicate invoice.

**F-021 — Provider can view provider invoices, customer cannot view provider endpoint**

- Actor: provider, customer.
- Route/API: `GET /api/v1/invoices/provider/my-invoices`.
- Expected:
  - Provider sees invoices for own provider account.
  - Customer without provider claim gets `403`/owner mismatch.
  - Provider cannot view another provider's invoice by id unless admin.

### Dispute filing (within 7 days of booking)

**F-022 — Customer files dispute within 7 days**

- Actor: userA.
- Route/API: `POST /api/v1/disputes`.
- Setup: completed paid booking/payment within dispute window.
- Expected:
  - `200 OK` with dispute dto.
  - Status `Open`.
  - Deadline equals filed time + 7 days.
  - Payment state machine reflects `Confirmed → Disputed` if exposed.
  - Admin queue can see dispute.

**F-023 — Customer cannot file dispute after 7 days**

- Actor: userA.
- Setup: completed booking older than 7 days.
- Expected:
  - `400/422` business validation failure.
  - No dispute row.
  - Payment remains confirmed/non-disputed.

**F-024 — Duplicate dispute for same booking/payment is rejected**

- Actor: userA.
- Steps: file dispute twice for same payment.
- Expected:
  - First succeeds; second returns conflict/invalid.
  - `GET /api/v1/disputes/my` shows one dispute.

**F-025 — Non-owner cannot dispute another customer's payment**

- Actor: userB.
- Steps: userB attempts to open dispute for userA payment id.
- Expected: `403` or not-found masking; no dispute created.

### Dispute resolution (admin only: Full Refund / Partial Refund / Platform Credit / Reject)

**F-026 — Admin moves dispute Open → UnderReview**

- Actor: admin.
- Route/API: `POST /api/v1/disputes/{id}/review`.
- Expected:
  - Status changes to `UnderReview`.
  - Repeating review is idempotent or returns clean invalid-state response, not `500`.

**F-027 — Customer cannot resolve own dispute**

- Actor: userA.
- Route/API: `POST /api/v1/disputes/{id}/resolve`.
- Expected:
  - `403 Forbidden`.
  - Status remains `Open` or `UnderReview`.
  - No refund/credit issued.

**F-028 — Admin resolves Full Refund**

- Actor: admin.
- Steps: move dispute under review, resolve as full refund (`RefundFull` in current enum; map to Full Refund scenario).
- Expected:
  - Dispute state `Resolved`.
  - Payment/refund flow creates or marks refund payment.
  - Customer-visible payment is `Refunded` or refund status exposed.
  - `RefundIssued`/refund integration event side effect observable where possible.

**F-029 — Admin resolves Partial Refund**

- Actor: admin.
- Steps: resolve under-review dispute with partial refund amount/notes.
- Expected:
  - Dispute `Resolved` with partial-refund resolution.
  - Refund amount is greater than 0 and less than original payment.
  - Decimal precision preserved.
  - Provider payout/earning adjusted or held as implemented.

**F-030 — Admin resolves Platform Credit / Compromise**

- Actor: admin.
- Current enum has `Compromise`; if UI/API labels it Platform Credit, test label mapping.
- Expected:
  - Dispute `Resolved`.
  - Credit/compromise notes are mandatory.
  - No cash refund unless amount is explicitly part of compromise.

**F-031 — Admin rejects dispute / No Refund**

- Actor: admin.
- Current enum has `NoRefund`; test UI label `Reject` maps to it.
- Expected:
  - Dispute `Resolved` or closed in favor of provider.
  - No refund payment created.
  - Provider payout remains eligible after escrow/hold rules.

**F-032 — Admin can escalate complex dispute**

- Actor: admin.
- Route/API: `POST /api/v1/disputes/{id}/escalate`.
- Expected: status `Escalated`; regular customer/provider cannot call endpoint.

### ProviderPaymentMethod CRUD

**F-033 — Provider creates BankAccount payout method**

- Actor: `guide-approved`.
- Route/API: `POST /api/v1/provider-payment-methods`.
- Body: `PaymentMethodType=BankAccount`, display name, account identifier/IBAN, bank name, `isDefault=true`.
- Expected:
  - `200 OK` with method dto.
  - Method belongs to current user.
  - `IsDefault=true`; `IsVerified=false` until admin verifies.

**F-034 — Provider creates mobile wallet payout method**

- Actor: guide/business/agency provider.
- Body: `JoMoPay`, `OrangeMoney`, or `ZainCash` with wallet account identifier.
- Expected:
  - Accepted values are actual enum values: `BankAccount`, `JoMoPay`, `OrangeMoney`, `ZainCash`.
  - Plan's old `BankTransfer` value is not required/should fail if submitted.

**F-035 — Provider updates own payment method**

- Actor: owner provider.
- Route/API: `PUT /api/v1/provider-payment-methods/{id}`.
- Expected: owner can update display name/account details; updated row appears in list.

**F-036 — Provider cannot update/delete another provider's method**

- Actor: different provider or customer.
- Expected: `403`/not found; target remains unchanged.

**F-037 — Provider deletes/deactivates payment method**

- Actor: owner provider.
- Route/API: `DELETE /api/v1/provider-payment-methods/{id}`.
- Expected: returns `true`; method disappears from active list or is marked inactive; payouts no longer use it.

**F-038 — Admin verifies provider payment method**

- Actor: admin.
- Route/API: `POST /api/v1/provider-payment-methods/{id}/verify`.
- Expected:
  - `IsVerified=true` after verify.
  - Provider/customer without verify permission gets `403`.
  - Verified method can be used by payout batch.

### CommissionRule lookup (tier by monthly revenue)

**F-039 — Admin creates non-overlapping commission rule**

- Actor: admin.
- Route/API: `POST /api/v1/commissions`.
- Expected:
  - Rule persisted with `decimal(19,4)` min/max and percentage.
  - Rule visible in `GET /api/v1/commissions`.

**F-040 — Overlapping commission rule is rejected**

- Actor: admin.
- Steps: create rule overlapping same tier/currency/monthly revenue window.
- Expected: `409 Conflict` or validation error; existing rule unchanged.

**F-041 — Admin updates commission percentage and notes**

- Actor: admin.
- Route/API: `PUT /api/v1/commissions/{id}`.
- Expected:
  - Updated percentage used by subsequent payout calculation.
  - Existing historical payout snapshots do not retroactively change.

**F-042 — Admin soft-deletes commission rule**

- Actor: admin.
- Route/API: `DELETE /api/v1/commissions/{id}?reason=...`.
- Expected:
  - Rule omitted from active lookup unless `includeInactive=true`.
  - Non-admin receives `403`.

## 2. NOT_BUILT

**F-043 — Unified PaymentMethod (bank + wallets) shell-only gap**

- Expected current result: no `/api/v1/payment-methods/*` unified customer/provider route should be treated as Built.
- Active implementation is `/api/v1/provider-payment-methods` with `ProviderPaymentMethod`.
- Test assertion: document as NOT_BUILT unless a real unified `PaymentMethod` entity/API appears.

**F-044 — CreditNote refund documentation missing**

- Trigger a full/partial refund and look for CreditNote API/table/UI.
- Expected current result: credit note is not available; invoice may be cancelled/updated but no `CreditNote` entity/API is expected.

**F-045 — GuideEarning entity missing**

- Use `/api/v1/finance/guide` and `/api/v1/finance/guide/summary` if present.
- Expected current result: query-level earnings may exist through `IGuideEarningReader`, but no persisted `GuideEarning` aggregate/table should be assumed.

**F-046 — MonthlyStatement table missing / DTO-only**

- Look for statement routes/tables/UI.
- Expected current result: monthly statement persisted table is NOT_BUILT; only future DTO/on-demand PDF is planned.

Top NOT_BUILT items:

1. Unified `PaymentMethod` entity/API (bank + wallets) — only `ProviderPaymentMethod` active.
2. `CreditNote` entity for refunds — missing.
3. `GuideEarning` aggregate/table — missing; earnings are query-level/service based.
4. `MonthlyStatement` persisted table — intentionally not built; DTO/PDF-on-demand only.

## 3. DEFERRED

**F-047 — Loyalty post-MVP remains inaccessible**

- Assert no active customer-facing loyalty accrual/redemption Finance workflow is required in MVP tests.

**F-048 — Referral post-MVP remains inaccessible**

- Assert no referral-code payout/credit workflow is required in Built tests.

**F-049 — Subscription post-MVP does not affect commission**

- Seed/alter subscription state if any shell exists; rerun commission lookup.
- Expected: commission remains revenue-tier based. Subscription-tier commission is deferred and should not be treated as current behavior.

## 4. Integration Events

### Publishes

**F-050 — `PaymentSucceeded` / current `PaymentCompletedIntegrationEvent` published once**

- Trigger payment success webhook.
- Expected: one payment-completed/succeeded integration side effect; duplicate webhook does not duplicate it.

**F-051 — `PayoutDisbursed` / current `PayoutCompletedIntegrationEvent` published once**

- Approve/complete payout through batch/gateway path.
- Expected: one payout-completed/disbursed integration side effect.

**F-052 — `DisputeResolved` published once**

- Resolve dispute as admin.
- Expected: one dispute-resolved side effect; repeat resolve is invalid/no-op.

**F-053 — `RefundIssued` / current refund initiated/completed events published once**

- Resolve full/partial refund or call refund API.
- Expected: one refund initiated and one refund completed/failed terminal event per refund transaction.

**F-054 — `CommissionCalculated` behavior observable via payout snapshot**

- Current contracts show commission rule upsert/delete events; if no explicit `CommissionCalculatedIntegrationEvent`, assert commission calculation through payout item snapshot and document event gap.

### Consumes

**F-055 — `BookingConfirmed` / booking-created expectation creates payment expectation**

- Publish/seed booking creation event.
- Expected: Finance creates `PaymentExpectation`; payment initiation can proceed.

**F-056 — `BookingCancelled` triggers refund path**

- Cancel eligible paid booking.
- Expected: Finance starts refund according to refund tiers and idempotency.

**F-057 — `TourCompleted` releases escrow after hold period**

- Mark tour/booking completed; advance time past escrow.
- Expected: payment becomes payout-eligible.

**F-058 — `ProviderApproved` enables payout-method/payout readiness**

- Approve provider then verify provider payment method.
- Expected: provider can receive payouts; unapproved/suspended provider payouts held.

## 5. Validation Matrix (5 commands missing FluentValidation per audit)

Audit/code currently shows validators for only:

- `InitiatePaymentCommandValidator`
- `RefundPaymentCommandValidator`
- `CreateCommissionRuleCommandValidator`

Missing FluentValidation coverage to assert with negative payload tests:

| Command | Current status | Playwright/API negative cases |
|---|---|---|
| `ProcessWebhookCommand` | Missing validator | Empty event type, empty gateway id, negative amount, unsupported currency should return clean `400/422`, not `500`. |
| `ApprovePayoutCommand` | Missing validator | Empty payout id should fail validation/not found cleanly; unauthenticated `401`; customer `403`. |
| `TriggerPayoutCommand` | Missing validator | Manual trigger has no body, but command should still guard auth and repeated trigger idempotency. |
| `UpdateCommissionRuleCommand` | Missing validator | Negative min revenue, max < min, percentage <= 0 or > 100 should fail. |
| `DeleteCommissionRuleCommand` | Missing validator | Empty id or excessive reason length should fail cleanly. |

Also cover grouped-command files without validator evidence:

- `CreateProviderPaymentMethodCommand` — missing/empty display name/account identifier, invalid enum.
- `UpdateProviderPaymentMethodCommand` — invalid ownership/id/default toggle.
- `DeleteProviderPaymentMethodCommand` — invalid id/ownership.
- `VerifyProviderPaymentMethodCommand` — non-admin forbidden; invalid id clean failure.
- Dispute commands (`OpenDisputeCommand`, `MarkDisputeUnderReviewCommand`, `ResolveDisputeCommand`, `EscalateDisputeCommand`) — reason/description/notes required and max length.

## 6. Auth Matrix (Anonymous=401, Customer for own payments only, Provider for own payouts, Admin all)

| Surface | Anonymous | Customer | Provider/Guide | Admin |
|---|---:|---:|---:|---:|
| `POST /api/v1/payments/initiate` | 401 | Own booking only | Own booking only if customer role permits | Allowed where permission exists |
| `POST /api/v1/payments/webhook` | Allowed only with valid signature | N/A | N/A | N/A |
| `POST /api/v1/payments/{id}/refund` | 401 | Own payment only if policy permits | Own provider payment only | All |
| `GET /api/v1/payments/{id}` | 401 | Own only | Own provider payments only | All |
| `GET /api/v1/payments/my-payments` | 401 | Own list | Own user list | Own/admin as implemented |
| `GET /api/v1/payments/admin/all` | 401 | 403 | 403 unless financial-admin | All |
| `GET /api/v1/payouts/provider` | 401 | 403/no provider claim | Own payouts | All via admin endpoints |
| `POST /api/v1/payouts/admin/trigger` | 401 | 403 | 403 | All |
| `POST /api/v1/payouts/{id}/approve` | 401 | 403 | 403 | All |
| `/api/v1/invoices/my-invoices` | 401 | Own | Own user invoices | Own/admin as implemented |
| `/api/v1/invoices/provider/my-invoices` | 401 | 403 | Own provider invoices | All via detail/admin permissions |
| `/api/v1/commissions/*` | 401 | 403 | 403 | All CRUD |
| `/api/v1/provider-payment-methods` | 401 | 403 unless permission | Own CRUD | Verify/all if permitted |
| `/api/v1/disputes` open/my | 401 | Own disputes | Own disputes/provider scenarios if permitted | All via admin queue |
| `/api/v1/disputes/{id}/review|resolve|escalate` | 401 | 403 | 403 | All |
| `/api/v1/finance/admin/dashboard` | 401 | 403 | 403 | All |

## 7. State Machine — Payment (Pending → Confirmed → Refunded/Disputed)

**F-059 — Payment happy path state sequence**

- Initiate payment → assert `Pending`.
- Send success webhook → assert `Confirmed`/completed equivalent.
- Download invoice → state remains confirmed.

**F-060 — Payment refund state sequence**

- Start from confirmed payment.
- Refund full amount.
- Assert terminal `Refunded` or refund-completed status.
- Replay refund webhook/event → no duplicate refund.

**F-061 — Payment dispute state sequence**

- Start from confirmed payment.
- Open dispute → assert payment is linked to dispute or marked disputed where exposed.
- Resolve no-refund → payment remains confirmed.
- Resolve refund → payment becomes refunded/partially refunded as applicable.

**F-062 — Invalid transitions fail cleanly**

- Refund pending payment, confirm failed payment, dispute refunded payment.
- Expected: `400/409/422` business errors, never `500`.

## 8. State Machine — Dispute (Open → UnderReview → Resolved)

**F-063 — Open → UnderReview → Resolved full refund**

- Assert exact statuses in API/DOM.
- Assert resolver admin id/time present where exposed.

**F-064 — Open → UnderReview → Resolved no refund**

- Assert no refund payment/credit side effect.

**F-065 — Open → Escalated path**

- Escalate with reason.
- Assert status `Escalated`; resolution endpoint after escalation follows business rule or rejects without `500`.

**F-066 — Closed/resolved dispute cannot be resolved twice**

- Resolve dispute then call resolve again.
- Expected: conflict/invalid state; no duplicate refund/event.

## 9. State Machine — Payout (Pending → Processing → Disbursed/Failed)

**F-067 — Pending payout approval path**

- Trigger payout → pending.
- Approve as admin → processing/approved.
- Complete gateway payout or fake gateway success → disbursed/completed.

**F-068 — Failed payout path retains retry/admin visibility**

- Simulate gateway payout failure.
- Expected: status `Failed`, failure reason visible, no customer-visible crash.

**F-069 — Provider suspension holds payouts**

- Publish/seed provider suspended event.
- Expected: pending payouts move to hold or are excluded from disbursement.

**F-070 — Payout detail ownership after each state**

- Provider can see own pending/processing/disbursed/failed details.
- Other provider/customer cannot.
- Admin can.

## 10. Currency & Money Tests (decimal(19,4), JOD/USD/EUR, locked at booking time)

**F-071 — JOD payment preserves four decimals**

- Booking amount `123.4567 JOD`.
- Expected API/DOM displays/returns `123.4567`; commission/net arithmetic matches four decimals.

**F-072 — USD payment preserves currency and four decimals**

- Booking amount `99.9900 USD`.
- Expected payment, invoice, payout item currency remains USD unless explicit conversion is configured.

**F-073 — EUR payment preserves currency and four decimals**

- Booking amount `88.8888 EUR`.
- Expected all Finance surfaces use EUR consistently.

**F-074 — Currency locked at booking time**

- Change current exchange/commission/currency config after booking but before payment.
- Expected payment/invoice uses booking-time amount/currency snapshot.

**F-075 — Mixed-currency payouts group separately**

- Eligible payouts in JOD and USD for same provider.
- Expected separate payout batches per currency.

## 11. Agency Split — verify 2 payouts per agency booking (agency commission + guide earning)

**F-076 — Agency booking splits post-platform remainder into agency + guide payouts**

- Actor: admin.
- Setup: agency booking gross `100.0000 JOD`, platform commission `15%`, agency split `20%` of post-platform remainder.
- Expected if agency split built:
  - Platform commission = `15.0000 JOD`.
  - Post-platform remainder = `85.0000 JOD`.
  - Agency payout item = `17.0000 JOD`.
  - Guide payout item = `68.0000 JOD`.
  - Two payout recipients are visible: agency + guide.
- Current audit note: agency split logic is listed as follow-up/blocked; if only one provider payout exists, mark scenario NOT_BUILT/BLOCKED, not failed Built behavior.

**F-077 — Independent guide booking creates one guide/provider payout**

- Setup: independent guide booking gross `100.0000 JOD`, platform commission `15%`.
- Expected: one payout item to guide for `85.0000 JOD`; no agency payout.

**F-078 — Agency split idempotency**

- Trigger payout twice for same agency booking.
- Expected: no duplicate agency or guide payout items.

## 12. Webhook Idempotency — replay test with same TransactionId yields no duplicate

**F-079 — Replay same signed webhook body**

- Same as F-005, but assert counts explicitly:
  - Payment rows for `GatewayPaymentId`: `1`.
  - Invoice rows for payment/booking: `1`.
  - Refund rows for refund webhook: `1`.
  - Integration/network-observable callbacks: one first-order side effect.

**F-080 — Replay different EventId same TransactionId**

- Same as F-006, asserting unique filtered `GatewayTransactionId` behavior and no duplicate state transition.

**F-081 — Concurrent duplicate webhook posts**

- Use `browser_evaluate` to fire two parallel `fetch` requests with identical event/transaction.
- Expected:
  - At most one non-idempotent success.
  - Other request is idempotent/no-op or conflict handled cleanly.
  - Final side-effect counts remain `1`.

## 13. Refund Tiers (FullRefundHours / PartialRefundHours / PartialRefundPercent; provider min 50% if 48h+; provider-cancel = 100% refund)

**F-082 — Customer cancellation before FullRefundHours gets 100% refund**

- Setup: paid booking cancelled before full-refund cutoff.
- Expected: full refund issued; payment/refund amount equals original payment.

**F-083 — Customer cancellation between FullRefundHours and PartialRefundHours gets partial refund**

- Setup: paid booking cancelled in partial window.
- Expected: refund amount equals `OriginalAmount * PartialRefundPercent`.

**F-084 — Customer cancellation after partial window gets no/low refund according to configured policy**

- Expected: no refund or configured minimum; clean business error/result if refund not allowed.

**F-085 — Provider minimum 50% refund when cancellation is 48h+ before booking**

- Setup: provider-side cancellation >=48h before scheduled time.
- Expected: customer refund is at least 50%, even if default tier would be lower.

**F-086 — Provider-cancelled booking always 100% refund**

- Setup: provider cancels confirmed paid booking.
- Expected: full refund; provider payout/earning not released.

**F-087 — Refund tier precision and currency**

- Use non-round amount such as `33.3333 JOD` and partial percent.
- Expected: amount rounded/serialized to `decimal(19,4)` consistently in refund payment, invoice/credit-note gap notes, admin payment list.

## 14. Known Divergence (commission model contradiction resolved → revenue-tier; PDF says subscription-tier — deferred per b3 audit)

1. **Commission model**: Tests must assert revenue-tier commission only. Subscription-tier commission from PDF is deferred/out of scope.
2. **ProviderPaymentMethod vs PaymentMethod**: Actual Built model is `ProviderPaymentMethod`; unified `PaymentMethod` remains NOT_BUILT.
3. **Dispute implementation**: Current plan/audit says Dispute is fully implemented, but endpoint surface has 6 endpoints, not the 8 aspirational routes in the workflow table. Do not fail for missing provider response/evidence endpoints unless product marks them Built.
4. **CreditNote**: Required for 10/10 but currently missing; refund tests should record gap rather than expect credit note output.
5. **GuideEarning**: Current guide earnings API may be derived from payment/payout data; no persisted `GuideEarning` entity should be assumed.
6. **MonthlyStatement**: No table by design; future DTO/on-demand PDF only.
7. **Threshold conflict**: User task requires min payout 10 JOD; Finance workflow notes 20 JOD for guides and 10 JOD for tour providers. Runtime tests should read config and flag mismatch as blocker.
8. **App not running**: Playwright execution is blocked until SQL connection string bug is fixed and API/Web are started.

## Scenario count

Total scenarios: **87** (`F-001` through `F-087`).

## Blockers

- App is not currently running.
- SQL connection string has known double-suffix bug: `Server=MOHAMMAD\SQLEXPRESS\\SQLEXPRESS`.
- Finance UI may not exist under `YallaJo.Web/Areas/Finance` or Admin Finance; API fallback via `browser_evaluate` is required.
- Payment gateway test-mode/sandbox credentials must be configured, or deterministic `FakePaymentGateway` must stay enabled.
- Seed bookings/payment expectations, escrow eligibility, agency split metadata, and verified provider payment methods are required before most scenarios can execute.
- Agency split is listed as follow-up/blocked in plans; mark split scenarios as NOT_BUILT/BLOCKED if current code still creates only one payout.
- Min payout threshold ambiguity: 10 JOD requested for Built tests vs plan note of 20 JOD for guides.
