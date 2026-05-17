# Finance — Critical Rules (F-R1..F-R12)

> Additive to INDEX §4. Tech Lead enforces in every PR review.

---

**F-R1 — Money type**
All currency amounts are `decimal(19,4)` with explicit `Currency` 3-letter ISO column ({JOD,USD,EUR} this sprint). Use `Money(Amount, Currency)` value object end-to-end. **Never use `double` or `float`.** Rounding: banker's `Math.Round(x, 2, MidpointRounding.ToEven)`. No cross-currency math without explicit FX conversion call (out of scope this sprint).

**F-R2 — Webhook authentication**
POST /payments/webhook is `.AllowAnonymous()`. **Authentication = HMAC-SHA256 signature** verification against `Finance:Gateway:WebhookSecret` BEFORE parsing the body. Reject 400 + `Error('Payment.WebhookSignatureMismatch', ...)` if invalid. Implementation: `IPaymentGateway.VerifyWebhookSignatureAsync` (PW-6). Log signature mismatches at Warning level with caller IP (rate-limit alerting downstream).

**F-R3 — Idempotency**
Webhook handler MUST be idempotent. UNIQUE constraint on `Payment.GatewayTransactionId` (nullable until first webhook). Repeat webhook for same transaction → handler reads existing Payment, returns 200 with `{ idempotent: true }`, NO state change, NO duplicate domain event raised.

**F-R4 — Escrow model**
Per PDF 2 §1.5: **payments flow into platform escrow, NOT directly to provider.** Implementation:
- POST /payments/initiate creates Payment with `RecipientAccount = "platform-escrow"`.
- Successful webhook moves Payment to Completed; **does not** create a Payout row.
- Booking completion (`booking.tour-booking.completed.v1` inbox) marks Payment with `EscrowReleaseEligibleAt = CompletedAt + 7 days` (the 7-day dispute window per PDF 2).
- `PayoutBatchingService` (T5) groups eligible payments into Payouts every Sunday midnight.

**F-R5 — Refund calculation**
The refund **amount** is computed by Booking module (refund policy snapshot — see Booking 02-critical-rules.md B-R5). Finance just executes the gateway call.
- POST /payments/{id}/refund accepts `{ amount, currency, reason }`. Currency MUST match original Payment.Currency.
- Amount cannot exceed `Payment.AmountTotal - Payment.RefundedTotal`. Validator error `Refund.AmountExceedsRefundable`.
- Refund itself is a separate `Payment` row with `Type = Refund`, `OriginalPaymentId = <id>`, `Amount = -<amount>` (negative for accounting double-entry).
- Failures bump `Refund.RetryCount`; RefundRetryService (T5) picks up `Refund.Status = Failed AND RetryCount < 3` every 15 min.
- Commission: per PDF 2 §1.5 "commission handling configurable retention" — default this sprint = **commission NOT refunded to provider on refund** (deducted from next payout cycle if already paid out). Config knob `Finance:Refund:CommissionRetentionPolicy` ∈ {RefundToProvider, RetainByPlatform}; default = RetainByPlatform.

**F-R6 — Commission tier resolution order**
Per booking → commission lookup walks (Booking sprint snapshot consumer pulls this from Finance via integration event):
1. Provider has explicit `Subscription.Tier` override (Phase 3 — stub returns null for now).
2. CommissionRule for `Tier=Free + Currency=Booking.Currency` if no subscription (THIS sprint default).
3. Fallback: hard-coded 15% if no matching rule.

**Commission % applied to DISCOUNTED price** (per PDF 2 §1.5). This sprint = booking total since no discounts yet (DiscountEvaluator stub returns None).

**F-R7 — Payout batching invariants**
- Run Sunday midnight UTC (`PayoutBatchingService` cron, T5).
- Aggregates `Payment.Status = Completed AND EscrowReleaseEligibleAt <= now AND NOT (linked to existing PayoutItem)`.
- Group by `(ProviderId, Currency)`.
- One Payout per (ProviderId, Currency) per batch.
- **Hold conditions** (skip provider in this batch, log + notify):
  - Provider has no verified `ProviderBankAccount` for the currency → Payout NOT created; emails provider weekly reminder via Messaging.
  - Provider has open Dispute on any payment in the batch → Payout created but `Status = Hold` and frozen amount = disputed total.
  - Provider's `NetTotal < Finance:Payout:MinAmountThreshold` (default 10 JOD equivalent) → Payout NOT created; rolls over to next batch.
- Each Payout starts `Status = Pending`. **Large payouts** (> 5000 of currency, configurable) require manual `POST /payouts/{id}/approve` by admin. Small payouts auto-trigger gateway call by Status = ReadyForPayout immediately.

**F-R8 — Invoice generation**
Invoice auto-generated when `PaymentCompletedDomainEvent` fires. **InvoiceNumber format**: `INV-{YYYYMM}-{seq6}` (seq6 = 6-digit zero-padded monthly sequence, restarted on month boundary, race-safe via `INSERT ... OUTPUT` SQL pattern or sequence object). One invoice per Payment. **PDF generation deferred to download time** — invoice metadata only persisted; PDF stream produced on `GET /invoices/{id}/download` via QuestPDF.

**F-R9 — Audit logging**
Every Payment + Payout state change emits an `AuditLogIntegrationEvent` for Analytics consumption (Analytics sprint will wire the inbox). For THIS sprint:
- `BookingFinanceAuditLogPersister` writes to a temporary `finance.AuditLogs` table.
- Fields: `EntityType, EntityId, Action, OldStatus, NewStatus, ActorUserId, ActorIp, OccurredAt, MetadataJson`.
- Payment / Payout values **redacted** in MetadataJson — only `{ amount, currency, status }`, NEVER card numbers / bank account numbers.

**F-R10 — Pagination**
Cursor-based per INDEX §4 R9. Same shape as Booking: opaque `cursor`, `pageSize` clamped [1,50] default 20, envelope `{items, nextCursor, totalCount?}`, totalCount opt-in via `?countTotal=true` (admin endpoints only — keeps user endpoints fast).

**F-R11 — Auth matrix**

| Endpoint | Permission | ICurrentUser use |
|---|---|---|
| POST /payments/initiate | `Payment.Create` | self-check booking.UserId == currentUser.UserId |
| POST /payments/webhook | `.AllowAnonymous()` | none — HMAC verified |
| POST /payments/{id}/refund | `Refund.Create` | self-check (user own booking) OR admin/provider |
| GET /payments/{id} | `Payment.Read` | self/provider/admin (ownership guard in handler) |
| GET /payments/my-payments | `Payment.Read` | self-filter `WHERE UserId = currentUser.UserId` |
| GET /payments/admin/all | `AdminFinanceDashboard.Read` | none |
| GET /invoices/my-invoices | `Invoice.Read` | self-filter |
| GET /invoices/provider/my-invoices | `Invoice.Read` | self-filter (provider role) |
| GET /invoices/{id} | `Invoice.Read` | self/provider/admin |
| GET /invoices/{id}/download | `Invoice.Download` | self/provider/admin |
| GET /payouts/admin/pending | `Payout.Read` (admin) | none |
| GET /payouts/provider | `Payout.Read` | self-filter (provider) |
| GET /payouts/{id} | `Payout.Read` | self-filter (provider) OR admin |
| POST /payouts/admin/trigger | `Payout.Trigger` | none |
| POST /payouts/{id}/approve | `Payout.Approve` | none |
| GET /commissions | `CommissionRule.Read` | none |
| POST /commissions | `CommissionRule.Create` | none |
| PUT /commissions/{id} | `CommissionRule.Update` | none |
| DELETE /commissions/{id} | `CommissionRule.Delete` | none |

**F-R12 — Error registry** (codes mapped to Outcome)

| Code | Outcome | Where |
|---|---|---|
| `Payment.NotFound` | NotFound | GET /payments/{id} |
| `Payment.BookingNotEligible` | Validation | initiate (booking not in AwaitingPayment) |
| `Payment.AlreadyInitiated` | Conflict | initiate (Payment already exists for booking) |
| `Payment.GatewayError` | ExternalServiceError | gateway returns non-2xx |
| `Payment.WebhookSignatureMismatch` | Forbidden | webhook |
| `Payment.WebhookEventUnknown` | Validation | webhook with unknown event type — log + 200 |
| `Refund.AmountExceedsRefundable` | Validation | refund |
| `Refund.PaymentNotCompleted` | Conflict | refund (payment still Pending/Failed) |
| `Refund.GatewayError` | ExternalServiceError | refund |
| `Invoice.NotFound` | NotFound | invoices |
| `Invoice.NotReady` | Conflict | download (PDF not yet rendered after async generation) |
| `Payout.NotFound` | NotFound | payouts |
| `Payout.AlreadyApproved` | Conflict | approve idempotency guard |
| `Payout.InsufficientNetAmount` | Validation | trigger (below MinAmountThreshold) |
| `Payout.ProviderBankAccountMissing` | Validation | trigger (no verified account) |
| `Payout.NotPending` | Conflict | approve (state guard) |
| `CommissionRule.OverlapTier` | Conflict | upsert (overlapping tier × currency × revenue range) |
| `CommissionRule.NotFound` | NotFound | get/update/delete |
| `CommissionRule.CannotDeleteInUse` | Conflict | delete if any pending Payout references this rule |
| `ProviderBankAccount.NotVerified` | Validation | (Phase 3 reference) |
