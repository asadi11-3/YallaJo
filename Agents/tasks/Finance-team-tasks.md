# Finance Module — Combined Sprint Task File

> **Sprint window:** Mon 2026-08-17 → Thu 2026-10-15 (9 weeks, 45 working days, 200 person-hours)
> **Combined from 12 separate files** in `Agents/tasks/Finance/` for single-file review.

---

## Table of Contents

- [00-README](#00-readme)
- [01-pre-work](#01-pre-work)
- [02-critical-rules](#02-critical-rules)
- [03-entities-matrix](#03-entities-matrix)
- [04-task-payments-initiate](#04-task-payments-initiate)
- [05-task-payments-webhook-refund](#05-task-payments-webhook-refund)
- [06-task-invoices-pdf](#06-task-invoices-pdf)
- [07-task-payouts](#07-task-payouts)
- [08-task-background-services](#08-task-background-services)
- [09-task-commission-rules](#09-task-commission-rules)
- [10-cross-cutting](#10-cross-cutting)
- [99-acceptance-gate](#99-acceptance-gate)

---

<a id="00-readme"></a>

## 00-README

> Source: `Finance/00-README.md`

# Finance Module — Sprint Manifest

> **Predecessor sprint:** [`../Booking/`](../Booking/00-README.md) (closes Sun 2026-08-09)
> **This sprint covers:** Wave 5 second half — Payments (escrow + webhook + refund), Payouts (admin trigger + weekly batch with tiered commission), Invoices (PDF), Commission Rules CRUD, plus 2 hosted services.
> **Difficulty vs Booking:** ⚙️⚙️⚙️⚙️ (4/5) — money handling + PCI proximity + idempotency-critical webhooks
> **Endpoint count:** **17 HTTP endpoints + 2 BG services**
> **Working-day estimate:** **40 working days × 4 devs ≈ 200 person-hours**
> **Window:** Mon **2026-08-17** → Thu **2026-10-15** (9 weeks, 45 working days incl. buffer)

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Owner |
|---|---|---|
| Pre-work cut | Fri 2026-08-14 17:00 | Tech Lead |
| Sprint kickoff (all-hands 60 min) | Mon **2026-08-17** 09:00 AST | Tech Lead |
| PW merge deadline | Tue 2026-08-18 17:00 | Tech Lead |
| Earliest task start | Wed 2026-08-19 09:00 | Per-task owner |
| Mid-sprint integration freeze | Sun 2026-09-21 17:00 | All devs |
| Hard PR cutoff | Wed 2026-10-14 17:00 | All devs |
| Hard merge-to-main cutoff | Thu 2026-10-15 17:00 | Tech Lead |
| Sprint retro + demo | Fri 2026-10-16 11:00 AST | Tech Lead |

Working week Sun→Thu (5 days). Daily standup 09:30 AST 15 min hard cap.

---

## 1. Working Days & Person-Hour Budget

| Metric | Value |
|---|---|
| Working days | 45 |
| Hours / day / dev | 5 (focus time after standup + ceremony) |
| Devs | 4 |
| Total hours | 900 |
| Task hours | 200 (real estimate below) |
| Review hours | 60 |
| Ceremony hours | 60 (kickoff + standup + retro) |
| Buffer | 580 (huge buffer because money-handling never goes to plan) |

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG services | Est hours | Hard deadline |
|---|---|---|---|---|---|---|
| **Mohammad** (LEAD) | Senior | T4 (Payouts), T5 (BG services) | 5 | 2 | 60 | Wed 2026-10-14 |
| **Mahmoud** | Intermediate | T1 (Payments init), T2 (Payments webhook + refund) | 6 | 0 | 60 | Wed 2026-10-14 |
| **Fadwa** | Intermediate | T3 (Invoices + PDF) | 3 | 0 | 36 | Wed 2026-10-14 |
| **NEW HIRE (TBD)** | Junior | T6 (Commission Rules CRUD) | 3 | 0 | 24 | Wed 2026-10-14 |
| Tech Lead | — | PW + review + PCI compliance audit | 0 | 0 | 20 | continuous |

If no junior is hired by 2026-08-17, Fadwa absorbs T6 and her T3 deadline shifts to Mon 2026-10-19. Master INDEX gets edited accordingly.

---

## 3. Task Summary

| Task | Owner | Hours | Endpoints | Description |
|---|---|---|---|---|
| [T1 — Payments initiate](04-task-payments-initiate.md) | Mahmoud | 28 | 1 | POST /payments/initiate — calls IPaymentGateway, creates Payment Pending |
| [T2 — Payments webhook + refund](05-task-payments-webhook-refund.md) | Mahmoud | 32 | 2 | POST /payments/webhook (idempotent), POST /payments/{id}/refund + 3 GET reads |
| [T3 — Invoices + PDF](06-task-invoices-pdf.md) | Fadwa | 36 | 3 | Auto-create invoice on payment success, GET my-invoices / provider/my-invoices / {id}/download (PDF via QuestPDF) |
| [T4 — Payouts](07-task-payouts.md) | Mohammad | 32 | 5 | GET admin/pending + provider + {id}, POST admin/trigger (weekly batch), POST {id}/approve |
| [T5 — BG services](08-task-background-services.md) | Mohammad | 28 | 0 | PayoutBatchingService (weekly Sun midnight UTC), RefundRetryService (15min) |
| [T6 — Commission Rules CRUD](09-task-commission-rules.md) | Junior/Fadwa | 24 | 3 | GET /commissions (admin), POST /commissions, PUT /commissions/{id}, DELETE — emits `finance.commission-rule.upserted/deleted.v1` consumed by Booking |
| **TOTAL** | | **180h** | **17** | |

(20h shortfall vs the 200 estimate is review/buffer; matches table 1.)

---

## 4. Integration Events Map

**This sprint emits (9 events):**
| Logical name | Producer task | Consumed by |
|---|---|---|
| `finance.payment.completed.v1` | T2 (webhook handler) | Booking inbox (confirms booking), Analytics (revenue tally), Messaging (receipt email) |
| `finance.payment.failed.v1` | T2 | Booking inbox (releases slot lock, restores capacity); Messaging (failure email) |
| `finance.refund.initiated.v1` | T2 | Booking inbox (updates booking refundedAmount) |
| `finance.refund.completed.v1` | T2 | Booking inbox + Messaging (refund confirmation email) |
| `finance.refund.failed.v1` | T5 (RefundRetryService after 3 attempts) | Messaging admin alert |
| `finance.invoice.generated.v1` | T3 | Messaging (invoice-available email) |
| `finance.payout.scheduled.v1` | T5 (PayoutBatchingService) | Messaging (provider payout summary email) |
| `finance.payout.completed.v1` | T4 (admin approve) | Messaging (provider notification with reference number) |
| `finance.commission-rule.upserted.v1` | T6 | Booking inbox (refreshes BookingCommissionSnapshot) |
| `finance.commission-rule.deleted.v1` | T6 | Booking inbox (removes BookingCommissionSnapshot) |

**This sprint consumes (5 events from Booking + Accounts):**
| Logical name | Producer | Handler in this sprint |
|---|---|---|
| `booking.tour-booking.created.v1` | Booking T4 | Stamps Payment.BookingId reference (Mohammad T4 pre-step) |
| `booking.tour-booking.completed.v1` | Booking T5 | Triggers escrow release scheduling (Mohammad T4) |
| `booking.tour-booking.cancelled.v1` | Booking T5 | Triggers refund creation (Mahmoud T2 inbox handler) |
| `accounts.provider.subscription-changed.v1` | Accounts (existing) | Re-evaluates commission tier on next payout (Mohammad T4) |
| `accounts.provider-bank-account.verified.v1` | Accounts (future) | Releases held payouts (deferred — flag as TODO if Accounts not ready) |

---

## 5. Out-of-Scope for This Sprint

| Item | Reason | When |
|---|---|---|
| Discount creation / approval / lifecycle | Phase 4 Feature 24 — needs its own sprint | Phase 4 |
| Loyalty points earning / redemption | Phase 3 Feature 17 | Phase 3 sprint |
| Subscription billing (Free / Basic / Premium) | Phase 3 Feature 16 | Phase 3 sprint |
| Dispute Center | Phase 3 Feature 19 | Phase 3 sprint |
| Referral system | Phase 3 Feature 17 | Phase 3 sprint |
| Subscription tier resolution in T4 commission lookup | Stub returns FreeTier; real lookup ships with Subscriptions sprint | Phase 3 |
| Multi-currency FX | Bookings stay in their booking currency through payout; cross-currency held as TODO | Phase 4 |

These features have entity rows already in `Finance.Domain/Entities/` (b5 — 19 entities); we leave them as stub property bags during this sprint, no endpoints, no handlers.

---

## 6. Reading Order for New Joiners

1. `Agents/agent-context.md` §0..§3 (rules + arch)
2. `Agents/guide.md` (code patterns)
3. `Agents/YallaJo.md` §Finance (endpoint spec)
4. `Agents/tasks/Phase1-Phase2-Completion-INDEX.md` §4 (16 critical rules)
5. `Agents/decisions/closed/ContentBlogs-ContentSeo-team-tasks.md` (template for sprint mechanics)
6. `Agents/decisions/closed/Booking/` (predecessor — same template)
7. **THIS folder** in order: `00-README` → `01-pre-work` → `02-critical-rules` → `03-entities-matrix` → tasks `04..09` → `10-cross-cutting` → `99-acceptance-gate`
8. PDFs: Endpoints.pdf §Wave 5, YallaJo Business Rules §1.5 (Payment & Refund) and §1.5 (Payout)

---

<a id="01-pre-work"></a>

## 01-pre-work

> Source: `Finance/01-pre-work.md`

# Finance — Pre-Work (PW-1..PW-7)

> **All PW items merged by Tue 2026-08-18 17:00. No feature work begins until this is green.** Mirrors the Booking pre-work pattern.

---

## PW-1: `FinanceUnitOfWork` delegates to SharedKernel UoW

**Current bug** (same shape as ContentBlogs/ContentSeo/Booking inherited):
```csharp
// Finance.Infrastructure/Persistence/FinanceUnitOfWork.cs
public async Task<int> SaveChangesAsync(CancellationToken ct) =>
    await _context.SaveChangesAsync(ct);
```
Domain events on Finance aggregates would never dispatch.

**Required fix:**
```csharp
internal sealed class FinanceUnitOfWork(
    FinanceDbContext context,
    IUnitOfWork<FinanceDbContext> sharedUow) : IFinanceUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct) =>
        await sharedUow.SaveChangesAsync(ct); // dispatches domain events BEFORE SaveChanges
    public DbContext Context => context;
}
```

**Acceptance gate:**
1. `tests/Finance.Tests.Unit/Persistence/FinanceUnitOfWorkDispatchesEventsTests.cs` — 2 tests: (a) Payment aggregate raising `PaymentCompletedDomainEvent` triggers `IPublisher.Publish` exactly once; (b) save count returns expected number.

---

## PW-2: IAggregateRoot markers + AuditableEntity upgrades

**In-scope aggregates** (rest stay BaseEntity / out-of-scope stubs):

| Entity | Current | After PW |
|---|---|---|
| `Payment` | BaseEntity stub 27L | `AuditableEntity, IAggregateRoot` |
| `Payout` | BaseEntity stub 23L | `AuditableEntity, IAggregateRoot` |
| `Invoice` (assume exists; if missing seed in PW) | n/a | `AuditableEntity, IAggregateRoot` |
| `CommissionRule` | BaseEntity | `AuditableEntity, IAggregateRoot` |
| `ProviderBankAccount` | BaseEntity | `AuditableEntity, IAggregateRoot` (provider needs verified bank acct before payout) |

**Junction-like / line-item entities** stay BaseEntity:
- `PayoutItem` (links Payout → Booking, per-line accounting)
- `InvoiceItem` / `InvoiceLineItem` (existing in b5 entity list — kept as owned collections under Invoice)
- `DiscountUsage` (only touched if a Discount sprint runs — keep stub here)

**Out-of-scope stubs** (do not touch): Discount, Dispute, DisputeEvidence, DisputeMessage, LoyaltyPoints, LoyaltyTransaction, Referral, Subscription, SubscriptionFeature, SubscriptionPlan, PlanFeature.

**Migration:** `FinanceAddAggregateRootAndAuditMembers`. Adds `IsDeleted`, `DeletedAt`, `RowVersion` to the 5 aggregates above. Tech Lead applies.

---

## PW-3: Domain event records

Seed these in `Finance.Domain/Events/` (one record per file, all under namespace `Finance.Domain.Events`):

| Event | Payload |
|---|---|
| `PaymentInitiatedDomainEvent` | PaymentId, BookingId, UserId, ProviderId, Amount, Currency |
| `PaymentCompletedDomainEvent` | PaymentId, BookingId, UserId, ProviderId, Amount, Currency, GatewayTransactionId |
| `PaymentFailedDomainEvent` | PaymentId, BookingId, ReasonCode, RawReason, OccurredAt |
| `RefundInitiatedDomainEvent` | RefundPaymentId, OriginalPaymentId, BookingId, Amount, Reason |
| `RefundCompletedDomainEvent` | RefundPaymentId, OriginalPaymentId, BookingId, Amount, GatewayRefundId |
| `RefundFailedDomainEvent` | RefundPaymentId, OriginalPaymentId, AttemptCount, FailureReason |
| `InvoiceGeneratedDomainEvent` | InvoiceId, BookingId, UserId, ProviderId, AmountTotal, AmountSubtotal, AmountTax, AmountDiscount |
| `PayoutBatchCreatedDomainEvent` | PayoutBatchId, BatchPeriodStart, BatchPeriodEnd, TotalAmount, ItemCount |
| `PayoutItemAddedDomainEvent` | PayoutId, BookingId, GrossAmount, CommissionAmount, NetAmount |
| `PayoutApprovedDomainEvent` | PayoutId, ApprovedByUserId, ApprovedAt, NetAmount, ProviderId |
| `PayoutCompletedDomainEvent` | PayoutId, ProviderId, NetAmount, GatewayPayoutId, CompletedAt |
| `PayoutFailedDomainEvent` | PayoutId, ProviderId, FailureReason, RetriedAt |
| `CommissionRuleUpsertedDomainEvent` | RuleId, Tier, MinMonthlyRevenue, MaxMonthlyRevenue, Currency, Percentage |
| `CommissionRuleDeletedDomainEvent` | RuleId |

14 records total. None call SaveChanges; they're plain `record` types implementing `IDomainEvent`.

---

## PW-4: Integration event records + registry

`Finance.Contracts/IntegrationEvents/` (one record per file):
- PaymentCompletedIntegrationEvent (mirrors domain payload + EventId)
- PaymentFailedIntegrationEvent
- RefundInitiatedIntegrationEvent
- RefundCompletedIntegrationEvent
- RefundFailedIntegrationEvent
- InvoiceGeneratedIntegrationEvent
- PayoutScheduledIntegrationEvent
- PayoutCompletedIntegrationEvent
- CommissionRuleUpsertedIntegrationEvent
- CommissionRuleDeletedIntegrationEvent

**10 logical names** registered in `IntegrationEventTypeRegistry`:
```
finance.payment.completed.v1
finance.payment.failed.v1
finance.refund.initiated.v1
finance.refund.completed.v1
finance.refund.failed.v1
finance.invoice.generated.v1
finance.payout.scheduled.v1
finance.payout.completed.v1
finance.commission-rule.upserted.v1
finance.commission-rule.deleted.v1
```

Reverse-parity test `IntegrationEventTypeRegistryParityTests` added (same pattern as Booking 11-cross-cutting.md §3).

---

## PW-5: Repository interfaces (Application layer)

Seed in `Finance.Application/Interfaces/Repositories/`:

| Interface | Custom finders |
|---|---|
| `IPaymentRepository` | `GetByGatewayTransactionIdAsync`, `GetByBookingIdAsync`, `GetPendingRefundsOlderThanAsync` |
| `IPayoutRepository` | `GetPendingApprovalAsync`, `GetByProviderAsync` (paginated), `GetByPeriodAsync` |
| `IPayoutItemRepository` | `GetByPayoutIdAsync` |
| `IInvoiceRepository` | `GetByUserAsync`, `GetByProviderAsync`, `GetByBookingIdAsync` |
| `ICommissionRuleRepository` | `GetForTierAsync(tier, currency)`, `GetOverlappingAsync` |
| `IProviderBankAccountRepository` | `GetDefaultForProviderAsync`, `GetVerifiedForProviderAsync` |

EF implementations in `Finance.Infrastructure/Repositories/`. Use primary-constructor style.

---

## PW-6: Payment gateway abstraction

`SharedKernel.Application/Abstractions/Payments/IPaymentGateway.cs`:
```csharp
public interface IPaymentGateway
{
    Task<InitiateResult> InitiateAsync(InitiateRequest request, CancellationToken ct);
    Task<bool> VerifyWebhookSignatureAsync(string rawBody, IDictionary<string, string> headers, CancellationToken ct);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct);
    Task<PayoutResult> PayoutAsync(PayoutRequest request, CancellationToken ct);
    string GatewayName { get; }
}

public record InitiateRequest(Guid PaymentId, decimal Amount, string Currency, string DescribedAs, Uri ReturnUrl, IDictionary<string, string> Metadata);
public record InitiateResult(string GatewayPaymentId, Uri? RedirectUrl, string? ClientSecret);
public record RefundRequest(string OriginalGatewayPaymentId, decimal Amount, string Currency, string Reason);
public record RefundResult(string GatewayRefundId, RefundStatus Status, string? FailureCode);
public record PayoutRequest(Guid PayoutId, string ProviderBankAccountRef, decimal Amount, string Currency);
public record PayoutResult(string GatewayPayoutId, PayoutGatewayStatus Status, string? FailureCode);
public enum RefundStatus { Completed, Pending, Failed }
public enum PayoutGatewayStatus { Completed, Pending, Failed }
```

**One stub implementation `FakePaymentGateway`** in `Finance.Infrastructure/Gateways/`:
- `InitiateAsync` returns deterministic `gw-pay-{paymentId}` ID + canned redirect URL.
- `VerifyWebhookSignatureAsync` verifies HMAC-SHA256 with a config-bound shared secret.
- `RefundAsync` returns Completed instantly.
- `PayoutAsync` returns Completed instantly.

**`StripeGateway` / `HyperPayGateway`** classes deferred to a parallel ticket; this sprint uses Fake. The `GatewayName` discriminator on Payment lets us swap later without schema changes.

DI: `services.AddSingleton<IPaymentGateway, FakePaymentGateway>()` in dev/staging; real impls register only when `Finance:Gateway:Provider == "Stripe"` etc.

---

## PW-7: FinanceFeatures + FinancePermissionCatalog

`Finance.Contracts/Authorization/FinanceFeatures.cs`:
```csharp
public static class FinanceFeatures
{
    public const string Payment            = "Payment";
    public const string Refund             = "Refund";
    public const string Invoice            = "Invoice";
    public const string Payout             = "Payout";
    public const string CommissionRule     = "CommissionRule";
    public const string ProviderBankAccount = "ProviderBankAccount";
    public const string AdminFinanceDashboard = "AdminFinanceDashboard";
}
```

`Finance.Contracts/Authorization/FinancePermissionCatalog.cs` enumerates:

| Feature | Actions |
|---|---|
| Payment | Create, Read |
| Refund | Create, Read |
| Invoice | Read, Download |
| Payout | Read, Trigger, Approve |
| CommissionRule | Create, Read, Update, Delete |
| ProviderBankAccount | Create, Update, Read, Verify |
| AdminFinanceDashboard | Read, Export, Refresh |

**Total: 22 permissions.** Expected boot log:
```
[INFO] PermissionSeeder discovered 8 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance
[INFO] PermissionSeeder inserted/verified 22 Finance permissions
```

**Webhook endpoint** uses `.AllowAnonymous()` (verified via HMAC signature instead of JWT) — documented in 02-critical-rules.md B-R2.

---

## PW-8: Test projects

`tests/Finance.Tests.Unit/` (xunit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0, EF InMemory 9.0.15) and `tests/Finance.IntegrationTests/` (Mvc.Testing 9.0.15 + WAF).

InternalsVisibleTo added to Finance.Application + Finance.Infrastructure csprojs.

`YallaJo.Api/Program.cs` already has `public partial class Program;` (added during Booking sprint) — no edit needed.

---

## PW-9: PCI compliance baseline

Tech Lead audits before merging any T1/T2 code:

- [ ] **No raw card numbers** stored or logged. `Finance.Application.Logging.PaymentRedactor` masks any field matching `/^\d{12,19}$/` to `****-####`.
- [ ] **Webhook signatures** verified before parsing body (T2 must reject 400 if signature mismatch).
- [ ] **TLS-only** webhook endpoint — `RequireHttpsMetadata = true` in Kestrel config; HTTP requests get 308.
- [ ] **Idempotency** on webhook via `GatewayTransactionId` UNIQUE constraint on Payment.
- [ ] **Audit log** for every Payment state change (`PaymentInitiated/Completed/Failed/Refunded`) routed to `analytics.AuditLogs` via integration event (Analytics sprint will consume; for now Finance writes to Booking's audit-log-shaped table or queues).
- [ ] **Secrets** (`Finance:Gateway:WebhookSecret`, `Finance:Gateway:ApiKey`) read from environment, NEVER appsettings.json.

---

<a id="02-critical-rules"></a>

## 02-critical-rules

> Source: `Finance/02-critical-rules.md`

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

---

<a id="03-entities-matrix"></a>

## 03-entities-matrix

> Source: `Finance/03-entities-matrix.md`

# Finance — Entities Matrix

> Reference: existing entities catalogued in `b5` summary (19 entities, 13 enums). This sprint touches 5 aggregates + 3 owned entities; leaves Discount/Dispute/Loyalty/Referral/Subscription untouched (stub property bags).

---

## 1. Aggregates in scope (PW-2 applies)

| Aggregate | File | Base class | Domain events raised | Owner | Task |
|---|---|---|---|---|---|
| `Payment` | `Finance.Domain/Entities/Payment.cs` | `AuditableEntity, IAggregateRoot` | PaymentInitiated, PaymentCompleted, PaymentFailed, RefundInitiated, RefundCompleted, RefundFailed | Mahmoud | T1, T2 |
| `Invoice` | `Finance.Domain/Entities/Invoice.cs` (NEW if not present; check PW step 0) | `AuditableEntity, IAggregateRoot` | InvoiceGenerated | Fadwa | T3 |
| `Payout` | `Finance.Domain/Entities/Payout.cs` | `AuditableEntity, IAggregateRoot` | PayoutBatchCreated, PayoutItemAdded, PayoutApproved, PayoutCompleted, PayoutFailed | Mohammad | T4, T5 |
| `CommissionRule` | `Finance.Domain/Entities/CommissionRule.cs` | `AuditableEntity, IAggregateRoot` | CommissionRuleUpserted, CommissionRuleDeleted | Junior/Fadwa | T6 |
| `ProviderBankAccount` | `Finance.Domain/Entities/ProviderBankAccount.cs` | `AuditableEntity, IAggregateRoot` | (Phase 3 — none this sprint) | — | (read-only consumer in T4) |

## 2. Owned/child entities (BaseEntity, no events)

| Entity | Owned by | Notes |
|---|---|---|
| `InvoiceItem` | Invoice | Line-item amount, type (Booking / Tax / Discount adjustment). EF owned-type. |
| `InvoiceLineItem` | InvoiceItem | If exists (b5 lists both — clarify in PW). If duplicate, drop one and standardize on `InvoiceItem`. |
| `PayoutItem` | Payout | Per-booking accounting row: BookingId, GrossAmount, CommissionAmount, NetAmount, CommissionRuleSnapshotId. |

## 3. Out-of-scope stubs (DO NOT TOUCH)

`Discount, DiscountUsage, Dispute, DisputeEvidence, DisputeMessage, LoyaltyPoints, LoyaltyTransaction, Referral, Subscription, SubscriptionFeature, SubscriptionPlan, PlanFeature` — leave as existing stub property bags. Reserved for Phase 3/4 sprints.

## 4. Integration events emitted (10 — PW-4)

| Name | Producer |
|---|---|
| `finance.payment.completed.v1` | Payment aggregate (T2 webhook) |
| `finance.payment.failed.v1` | Payment aggregate (T2 webhook) |
| `finance.refund.initiated.v1` | Payment aggregate (T2 refund) |
| `finance.refund.completed.v1` | Payment aggregate (T2 refund + T5 retry) |
| `finance.refund.failed.v1` | T5 RefundRetryService after 3 attempts |
| `finance.invoice.generated.v1` | Invoice aggregate (T3 auto-creation in PaymentCompleted handler) |
| `finance.payout.scheduled.v1` | T5 PayoutBatchingService |
| `finance.payout.completed.v1` | Payout aggregate (T4 approve) |
| `finance.commission-rule.upserted.v1` | CommissionRule aggregate (T6) |
| `finance.commission-rule.deleted.v1` | CommissionRule aggregate (T6) |

## 5. Integration events consumed (5)

| Name | Producer | Handler in this sprint |
|---|---|---|
| `booking.tour-booking.created.v1` | Booking T4 | T1 step: pre-populates Payment expected metadata (provider, currency, amount, escrow target) |
| `booking.tour-booking.completed.v1` | Booking T5 | T4 step: marks Payment.EscrowReleaseEligibleAt = CompletedAt + 7 days |
| `booking.tour-booking.cancelled.v1` | Booking T5 | T2 step: auto-creates refund record per cancellation payload (`RefundPercentage` from policy snapshot × Payment.AmountTotal) |
| `accounts.provider.subscription-changed.v1` | Accounts (existing) | T4 step: re-evaluates commission tier for provider's NEXT payout (no retroactive change) |
| `accounts.provider-bank-account.verified.v1` | Accounts (future) | TASK 4 stub handler — logs warning if event not received in 30 days for any held payout. Real release flow ships with Accounts module sprint. |

## 6. Value objects + enums extended

| VO | Definition |
|---|---|
| `Money(decimal Amount, string Currency)` | 3-letter ISO. `Add`/`Subtract` require same Currency or throws. Static `Money.Zero(currency)`. |
| `GatewayReference(string Provider, string TransactionId)` | Owned on Payment for traceability. |
| `RefundReason` enum | `UserCancellation, ProviderCancellation, ForceMajeureAdminOverride, PaymentDispute, Other` |
| `PayoutStatus` enum | extend existing `{Pending, ReadyForPayout, Hold, Completed, Failed, ManuallyResolved}` |
| `PaymentStatus` enum | `{Pending, Completed, Failed, RefundedPartial, RefundedFull}` |
| `PaymentType` enum (NEW) | `{Booking, Refund}` — Refund payments are negative-amount sibling rows |

## 7. Persistence layout (FinanceDbContext)

13 EF configurations: PaymentConfiguration, InvoiceConfiguration, InvoiceItemConfiguration (owned), PayoutConfiguration, PayoutItemConfiguration, CommissionRuleConfiguration, ProviderBankAccountConfiguration, FinanceInboxMessageConfiguration, FinanceOutboxMessageConfiguration + 4 untouched stub configs (Discount/Dispute/Loyalty/Subscription read-only DbSet for Phase 3 reuse).

## 8. Migration sequence

| # | Name | Owner | Task |
|---|---|---|---|
| 1 | `FinanceAddAggregateRootAndAuditMembers` | Tech Lead | PW-2 |
| 2 | `FinanceCreatePaymentIndexes` | Mahmoud | T1 (UNIQUE on GatewayTransactionId filtered WHERE NOT NULL; IX on BookingId + Status) |
| 3 | `FinanceAddInvoiceTable` | Fadwa | T3 (if Invoice didn't exist as entity yet) — InvoiceNumber UNIQUE |
| 4 | `FinanceAddPayoutAndPayoutItems` | Mohammad | T4 (IX on ProviderId + Status; IX on Status + CreatedAt filtered Status='ReadyForPayout') |
| 5 | `FinanceAddCommissionRuleConstraints` | Junior/Fadwa | T6 (UNIQUE filtered (Tier, Currency, MinMonthlyRevenue, MaxMonthlyRevenue) WHERE IsDeleted = 0) |
| 6 | `FinanceAddAuditLogTable` | Mahmoud | T1/T2 (temporary until Analytics sprint takes over) |

---

<a id="04-task-payments-initiate"></a>

## 04-task-payments-initiate

> Source: `Finance/04-task-payments-initiate.md`

# TASK 1 — POST /payments/initiate

> **Owner:** Mahmoud — **Hours:** 28h — **Hard deadline:** Sun **2026-09-13 17:00**
> **Earliest start:** Wed 2026-08-19 (after PW)
> **Endpoints:** 1 + 1 inbox handler
> **Depends on:** PW-3, PW-4, PW-5, PW-6 (gateway)

---

## 1. Endpoint Surface

| Method | Path | Permission | Returns |
|---|---|---|---|
| POST | `/api/v1/payments/initiate` | `Payment.Create` | 201 + `{ paymentId, gatewayPaymentId, redirectUrl?, clientSecret?, expiresAt }` |

**Inbox handler** (NOT an HTTP endpoint):
- `BookingTourBookingCreatedHandler` — consumes `booking.tour-booking.created.v1` → seeds a Payment expectation row (status `Awaiting`) tied to BookingId. This row gets PROMOTED to actual Payment when `POST /payments/initiate` is called. Avoids the order-of-operations problem of "booking created but no Payment yet exists to look up".

---

## 2. Request / Response

```http
POST /api/v1/payments/initiate
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "bookingId": "01956f60-...",
  "paymentMethod": "Card",                  // {Card, ApplePay, GooglePay, BankTransfer}
  "returnUrl": "https://yallajo.com/booking/confirm"
}
```

```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "paymentId": "01956f6a-...",
  "gatewayPaymentId": "gw-pay-01956f6a-...",
  "redirectUrl": "https://fake-gateway/checkout/abc",   // when redirect-based gateway
  "clientSecret": null,                                 // when SDK-based gateway (Stripe pattern)
  "expiresAt": "2026-09-01T10:15:00Z"
}
```

---

## 3. Validation (FluentValidation)

- `bookingId` not empty Guid
- `paymentMethod` ∈ enum
- `returnUrl` valid HTTPS URL within configured allow-list (`Finance:Gateway:ReturnUrlAllowedHosts`)
- `Authorization` Bearer valid (handled by JWT middleware)

---

## 4. Handler Flow

```csharp
public async Task<Result<InitiatePaymentResponse>> Handle(InitiatePaymentCommand cmd, CancellationToken ct)
{
    // 1. Look up booking via snapshot (we DON'T cross-call Booking module — read snapshot from inbox)
    var bookingExpectation = await _paymentRepo.GetExpectationByBookingIdAsync(cmd.BookingId, ct);
    if (bookingExpectation is null)
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.BookingNotEligible", "No booking expectation found"), Outcome.Validation);

    // 2. Self-ownership check
    if (bookingExpectation.UserId != _currentUser.UserId)
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.OwnerMismatch", "Not your booking"), Outcome.Forbidden);

    // 3. Booking must be in AwaitingPayment state (per snapshot)
    if (bookingExpectation.Status != "AwaitingPayment")
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.BookingNotEligible", "Booking not in AwaitingPayment"), Outcome.Conflict);

    // 4. Idempotency: if a Payment already exists for this booking that's not Failed, reuse it
    var existing = await _paymentRepo.GetActiveByBookingIdAsync(cmd.BookingId, ct);
    if (existing is not null && existing.Status is PaymentStatus.Pending)
        return Result.Success(MapToResponse(existing)); // 200 with existing details

    // 5. Create Payment aggregate via factory
    var payment = Payment.Initiate(
        bookingId: cmd.BookingId,
        userId: bookingExpectation.UserId,
        providerId: bookingExpectation.ProviderId,
        amount: new Money(bookingExpectation.AmountTotal, bookingExpectation.Currency),
        paymentMethod: cmd.PaymentMethod,
        gatewayProvider: _gateway.GatewayName,
        timeProvider: _timeProvider);
    // Raises PaymentInitiatedDomainEvent

    // 6. Call gateway. Wrap in Polly retry (Polly v8 — INDEX §4 R12 lets us catch in Infrastructure).
    var initRequest = new InitiateRequest(
        PaymentId: payment.Id,
        Amount: payment.AmountTotal.Amount,
        Currency: payment.AmountTotal.Currency,
        DescribedAs: $"YallaJo booking {payment.BookingId}",
        ReturnUrl: new Uri(cmd.ReturnUrl),
        Metadata: new Dictionary<string,string> { ["BookingId"] = payment.BookingId.ToString(), ["UserId"] = payment.UserId.ToString() });

    InitiateResult gw;
    try { gw = await _gateway.InitiateAsync(initRequest, ct); }
    catch (Exception ex)
    {
        // Polly already retried; this is final
        _logger.LogError(ex, "Gateway initiate failed for payment {PaymentId}", payment.Id);
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.GatewayError", "Payment gateway unavailable"), Outcome.ExternalServiceError);
    }

    payment.StampGatewayInitiation(gw.GatewayPaymentId, gw.RedirectUrl?.ToString(), gw.ClientSecret);

    // 7. Persist + emit outbox + invalidate cache
    await _paymentRepo.AddAsync(payment, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    await _cache.RemoveByTagAsync($"payments:booking:{payment.BookingId}", ct);

    return Result.Success(MapToResponse(payment));
}
```

---

## 5. Domain methods on `Payment` (T1 deliverables)

```csharp
public static Payment Initiate(Guid bookingId, Guid userId, Guid providerId, Money amount, PaymentMethod paymentMethod, string gatewayProvider, TimeProvider timeProvider);
public Result StampGatewayInitiation(string gatewayPaymentId, string? redirectUrl, string? clientSecret);
public Result MarkExpired(DateTime nowUtc);  // for T2 webhook timeout case
```

Factory `Initiate` raises `PaymentInitiatedDomainEvent`.

---

## 6. Cache & Tags

| Cache | Key | TTL | Tag |
|---|---|---|---|
| Payment-by-id | `payment:{paymentId}` | 10 min | `payment:{paymentId}`, `payments:user:{userId}` |
| Payment-by-booking lookup | `payments:booking:{bookingId}` | 1 min | `payments:booking:{bookingId}`, `payments:user:{userId}` |

Initiate command invalidates `payments:booking:{bookingId}` after save.

---

## 7. Inbox Handler — `BookingTourBookingCreatedHandler`

```csharp
internal sealed class BookingTourBookingCreatedHandler(
    IFinanceInboxStore inbox,
    IPaymentExpectationRepository expectationRepo,
    IFinanceUnitOfWork uow,
    ILogger<BookingTourBookingCreatedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventEnvelope<BookingTourBookingCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventEnvelope<BookingTourBookingCreatedIntegrationEvent> e, CancellationToken ct)
    {
        if (await inbox.HasBeenProcessedAsync(e.EventId, ct)) return;

        var ev = e.Payload;
        var expectation = PaymentExpectation.Create(
            bookingId: ev.BookingId,
            userId: ev.UserId,
            providerId: ev.ProviderId,
            amount: new Money(ev.TotalAmount, ev.Currency),
            status: "AwaitingPayment",
            createdAt: timeProvider.GetUtcNow().UtcDateTime);

        await expectationRepo.AddAsync(expectation, ct);
        inbox.MarkAsProcessed(e.EventId);
        await uow.SaveChangesAsync(ct);  // single SaveChanges
    }
}
```

`PaymentExpectation` is a lightweight read-snapshot entity (BaseEntity, no events). Lives in `Finance.Infrastructure/Persistence/Snapshots/`. Owned by Finance, no migration script needed beyond the table.

---

## 8. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `Payment.Initiate` factory + `StampGatewayInitiation` + `MarkExpired` + tests | 4 | 2026-08-21 |
| 2 | `PaymentExpectation` snapshot entity + repo + config + migration | 3 | 2026-08-24 |
| 3 | `BookingTourBookingCreatedHandler` inbox handler + test | 3 | 2026-08-26 |
| 4 | `InitiatePaymentCommand` + handler + validator | 5 | 2026-08-31 |
| 5 | Gateway DI registration + `FakePaymentGateway` impl + Polly retry policy | 4 | 2026-09-02 |
| 6 | Endpoint wiring + permission attribute + Swagger XML doc | 2 | 2026-09-03 |
| 7 | Cache builders + tag invalidation | 1 | 2026-09-03 |
| 8 | Unit tests (10+) covering validators, handler branches, factory invariants | 3 | 2026-09-08 |
| 9 | Integration test: booking created → expectation row → POST initiate succeeds + outbox row | 2 | 2026-09-10 |
| 10 | PR review fixes | 1 | 2026-09-13 |
| **Total** | | **28h** | |

---

## 9. Edge cases

1. Repeat POST initiate for same booking when active Payment exists → return existing details, do NOT create duplicate.
2. Booking auto-expires (Booking BG service) while initiate in flight → gateway call succeeds, but our state will be reconciled on `booking.tour-booking.payment-expired.v1` inbox consumer (T2) which cancels the gateway side and marks Payment Failed.
3. `PaymentExpectation` not yet seeded (booking event arrived after initiate due to inbox lag) → 422 with `Payment.BookingNotEligible`, user retries.
4. Currency mismatch (booking snapshot in JOD, user override URL claims USD) → reject — we always use the snapshot currency, never trust request body for amount/currency.
5. Network partition on gateway call → Polly retries 3× with exponential backoff; final failure returns 502.
6. Gateway returns 200 but no `GatewayPaymentId` → treat as failure, log + return 502.

---

<a id="05-task-payments-webhook-refund"></a>

## 05-task-payments-webhook-refund

> Source: `Finance/05-task-payments-webhook-refund.md`

# TASK 2 — Webhook + Refund + Payment Reads

> **Owner:** Mahmoud — **Hours:** 32h — **Hard deadline:** Sun **2026-09-27 17:00**
> **Earliest start:** Mon 2026-09-14 (after T1)
> **Endpoints:** 5 (webhook, refund, GET /payments/{id}, GET /my-payments, GET /admin/all)
> **Depends on:** T1 (Payment aggregate factory)

---

## 1. Endpoint Surface

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/payments/webhook` | `.AllowAnonymous()` | HMAC-verified. **F-R2.** |
| 2 | POST | `/api/v1/payments/{id}/refund` | `Refund.Create` | Admin or self/provider with cancel reason |
| 3 | GET | `/api/v1/payments/{id}` | `Payment.Read` | Self/provider/admin ownership |
| 4 | GET | `/api/v1/payments/my-payments` | `Payment.Read` | Self-filter cursor |
| 5 | GET | `/api/v1/payments/admin/all` | `AdminFinanceDashboard.Read` | Filters: userId, providerId, status, dateRange, currency |

Plus **1 inbox handler** `BookingTourBookingCancelledHandler` consumes `booking.tour-booking.cancelled.v1` → auto-creates Refund per snapshot policy `RefundPercentage × Payment.AmountTotal`.

---

## 2. POST /payments/webhook

### Request shape (gateway-agnostic envelope)
```http
POST /api/v1/payments/webhook
X-Signature: hmac-sha256=<hex>
X-Gateway: Fake               # or "Stripe", "HyperPay"
Content-Type: application/json

{
  "eventId": "evt_abc123",
  "eventType": "payment.succeeded",     // or "payment.failed", "refund.succeeded", "refund.failed"
  "occurredAt": "2026-09-01T10:14:32Z",
  "data": {
    "gatewayPaymentId": "gw-pay-...",
    "amount": 120.00,
    "currency": "JOD",
    "failureCode": null,
    "failureMessage": null
  }
}
```

### Handler flow
```csharp
public async Task<Result> Handle(WebhookCommand cmd, CancellationToken ct)
{
    // F-R2: verify signature against raw body BEFORE we even parse it.
    var verified = await _gateway.VerifyWebhookSignatureAsync(cmd.RawBody, cmd.Headers, ct);
    if (!verified)
        return Result.Failure(new Error("Payment.WebhookSignatureMismatch", "HMAC mismatch"), Outcome.Forbidden);

    // F-R3: idempotency — short-circuit on duplicate event delivery
    if (await _webhookInbox.HasBeenProcessedAsync(cmd.EventId, ct))
        return Result.Success();  // 200 noop

    // dispatch by event type
    switch (cmd.EventType)
    {
        case "payment.succeeded":
            await HandlePaymentSucceededAsync(cmd, ct);
            break;
        case "payment.failed":
            await HandlePaymentFailedAsync(cmd, ct);
            break;
        case "refund.succeeded":
            await HandleRefundSucceededAsync(cmd, ct);
            break;
        case "refund.failed":
            await HandleRefundFailedAsync(cmd, ct);
            break;
        default:
            _logger.LogWarning("Unknown webhook event type {Type}", cmd.EventType);
            // still record as processed so retries don't spam
            break;
    }

    _webhookInbox.MarkAsProcessed(cmd.EventId);
    await _uow.SaveChangesAsync(ct);
    return Result.Success();
}
```

### `HandlePaymentSucceededAsync`
```csharp
private async Task HandlePaymentSucceededAsync(WebhookCommand cmd, CancellationToken ct)
{
    var payment = await _paymentRepo.GetByGatewayTransactionIdAsync(cmd.GatewayPaymentId, ct);
    if (payment is null)
    {
        _logger.LogError("Webhook for unknown gw payment {GwId}", cmd.GatewayPaymentId);
        return;  // 200 — gateway will stop retrying if we return 2xx
    }

    // State guard: only Pending payments can transition to Completed
    var result = payment.MarkCompleted(cmd.GatewayTransactionId, _timeProvider.GetUtcNow().UtcDateTime);
    if (result.IsFailure) { _logger.LogWarning("MarkCompleted failed: {Code}", result.Error?.Code); return; }
    // Raises PaymentCompletedDomainEvent → outbox `finance.payment.completed.v1` (consumed by Booking inbox to confirm booking)
    // → Invoice generation also kicked off via T3 InvoiceGeneratorOnPaymentCompleted domain event handler

    await _cache.RemoveByTagAsync($"payment:{payment.Id}", ct);
    await _cache.RemoveByTagAsync($"payments:user:{payment.UserId}", ct);
}
```

### `HandlePaymentFailedAsync`
Marks Payment as Failed, raises `PaymentFailedDomainEvent` → `finance.payment.failed.v1` → Booking inbox releases SlotLock + restores capacity.

### `HandleRefundSucceededAsync` / `HandleRefundFailedAsync`
Look up Refund-typed Payment row by GatewayRefundId. Mark Completed / Failed. Raise corresponding events.

---

## 3. POST /payments/{id}/refund

### Request
```http
POST /api/v1/payments/01956f6a.../refund
Authorization: Bearer <jwt>

{
  "amount": 60.00,
  "currency": "JOD",
  "reason": "UserCancellation",         // RefundReason enum
  "notes": "Free text up to 500 chars"
}
```

### Handler flow
```csharp
public async Task<Result<RefundResponse>> Handle(RefundPaymentCommand cmd, CancellationToken ct)
{
    var original = await _paymentRepo.GetByIdAsync(cmd.PaymentId, ct);
    if (original is null) return Result.Failure<RefundResponse>(new Error("Payment.NotFound", ""), Outcome.NotFound);
    if (original.Status is not PaymentStatus.Completed)
        return Result.Failure<RefundResponse>(new Error("Refund.PaymentNotCompleted", ""), Outcome.Conflict);

    // ownership / role-based check
    if (!_currentUser.IsInRole("Admin") && original.UserId != _currentUser.UserId && original.ProviderId != _currentUser.ProviderId)
        return Result.Failure<RefundResponse>(new Error("Payment.OwnerMismatch", ""), Outcome.Forbidden);

    if (cmd.Currency != original.AmountTotal.Currency)
        return Result.Failure<RefundResponse>(new Error("Refund.CurrencyMismatch", ""), Outcome.Validation);

    var refundable = original.AmountTotal.Amount - original.RefundedTotal;
    if (cmd.Amount <= 0 || cmd.Amount > refundable)
        return Result.Failure<RefundResponse>(new Error("Refund.AmountExceedsRefundable", $"Refundable left: {refundable}"), Outcome.Validation);

    // Create a sibling Payment row with Type=Refund (negative amount tracked via Type discriminator, value stored absolute)
    var refund = original.CreateRefund(amount: cmd.Amount, reason: cmd.Reason, notes: cmd.Notes, timeProvider: _timeProvider);
    // Raises RefundInitiatedDomainEvent → outbox finance.refund.initiated.v1

    // Call gateway
    RefundResult gw;
    try { gw = await _gateway.RefundAsync(new RefundRequest(original.GatewayTransactionId, cmd.Amount, cmd.Currency, cmd.Reason.ToString()), ct); }
    catch (Exception ex) { _logger.LogError(ex, "Refund gateway error"); refund.MarkFailed("GatewayError"); }

    if (gw is { Status: RefundStatus.Completed })
        refund.MarkCompleted(gw.GatewayRefundId, _timeProvider.GetUtcNow().UtcDateTime);
    else if (gw is { Status: RefundStatus.Pending })
        { /* leave pending; webhook will move it forward */ }
    else
        refund.MarkFailed(gw?.FailureCode ?? "Unknown");

    await _paymentRepo.AddAsync(refund, ct);
    await _uow.SaveChangesAsync(ct);
    await _cache.RemoveByTagAsync($"payment:{cmd.PaymentId}", ct);
    return Result.Success(new RefundResponse(refund.Id, refund.Status, gw?.GatewayRefundId));
}
```

### Domain methods on `Payment` (T2 deliverables — additive to T1)
```csharp
public Result MarkCompleted(string gatewayTxId, DateTime occurredAtUtc);
public Result MarkFailed(string reasonCode, string? rawMessage);
public Payment CreateRefund(decimal amount, RefundReason reason, string? notes, TimeProvider timeProvider);   // factory for sibling row
public Result MarkRefundCompleted(string gatewayRefundId, DateTime occurredAtUtc);
public Result MarkRefundFailed(string failureCode);
public decimal RefundedTotal { get; private set; }   // updated on each refund.complete
```

---

## 4. Inbox handler — `BookingTourBookingCancelledHandler`

```csharp
internal sealed class BookingTourBookingCancelledHandler(...) : INotificationHandler<IntegrationEventEnvelope<BookingTourBookingCancelledIntegrationEvent>>
{
    public async Task Handle(IntegrationEventEnvelope<BookingTourBookingCancelledIntegrationEvent> e, CancellationToken ct)
    {
        if (await _inbox.HasBeenProcessedAsync(e.EventId, ct)) return;
        var ev = e.Payload;
        if (ev.RefundPercentage <= 0m)
        {
            _logger.LogInformation("Cancellation {BookingId} has 0% refund — no Refund row created", ev.BookingId);
            _inbox.MarkAsProcessed(e.EventId);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        var payment = await _paymentRepo.GetActiveByBookingIdAsync(ev.BookingId, ct);
        if (payment is null || payment.Status != PaymentStatus.Completed)
        {
            _logger.LogWarning("Cancellation refund cannot proceed — payment not completed");
            _inbox.MarkAsProcessed(e.EventId);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        var refundAmount = Math.Round(payment.AmountTotal.Amount * (ev.RefundPercentage / 100m), 2, MidpointRounding.ToEven);
        var reason = ev.ProviderInitiated ? RefundReason.ProviderCancellation : RefundReason.UserCancellation;
        if (ev.ForceMajeureOverride) reason = RefundReason.ForceMajeureAdminOverride;

        var refund = payment.CreateRefund(refundAmount, reason, ev.AdminNotes, _timeProvider);
        await _paymentRepo.AddAsync(refund, ct);

        // Call gateway eagerly (don't wait for explicit /refund endpoint)
        try {
            var gw = await _gateway.RefundAsync(new RefundRequest(payment.GatewayTransactionId, refundAmount, payment.AmountTotal.Currency, reason.ToString()), ct);
            if (gw.Status == RefundStatus.Completed) refund.MarkRefundCompleted(gw.GatewayRefundId, _timeProvider.GetUtcNow().UtcDateTime);
            else if (gw.Status == RefundStatus.Failed) refund.MarkRefundFailed(gw.FailureCode ?? "Unknown");
        } catch (Exception ex) {
            _logger.LogError(ex, "Auto-refund gateway error");
            refund.MarkRefundFailed("GatewayError");
        }

        _inbox.MarkAsProcessed(e.EventId);
        await _uow.SaveChangesAsync(ct);
    }
}
```

---

## 5. GET endpoints — quick summary

- **GET /payments/{id}** — `{ id, bookingId, amount, currency, status, paymentMethod, refundedTotal, createdAt, completedAt? }`, includes nested `refunds: [...]` array.
- **GET /my-payments** — cursor-paginated, filter by `?status=Completed|Pending|Failed&fromDate=&toDate=`.
- **GET /admin/all** — same shape + `userId, providerId, currency` filters. Cache 30s.

All three implement `ICacheableQuery` per INDEX §4 R10.

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Domain methods (MarkCompleted/Failed/CreateRefund/MarkRefundCompleted/Failed) + tests | 5 | 2026-09-15 |
| 2 | Webhook signature verification (`VerifyWebhookSignatureAsync` impl) + IPaymentWebhookInbox dedupe | 4 | 2026-09-16 |
| 3 | Webhook handler dispatch + 4 sub-handlers (succeeded/failed × payment/refund) | 6 | 2026-09-19 |
| 4 | POST /refund endpoint + handler + validator | 5 | 2026-09-22 |
| 5 | `BookingTourBookingCancelledHandler` inbox handler + test | 3 | 2026-09-23 |
| 6 | 3 GET endpoints + queries + cache + tag builders | 4 | 2026-09-24 |
| 7 | Unit tests for webhook idempotency, refund amount validation, ownership | 3 | 2026-09-26 |
| 8 | Integration test: webhook → Booking inbox consumes → booking Confirmed | 1 | 2026-09-26 |
| 9 | PR review | 1 | 2026-09-27 |
| **Total** | | **32h** | |

---

## 7. Edge cases

1. Webhook arrives BEFORE Payment row written (race with T1's SaveChanges) → `GetByGatewayTransactionIdAsync` returns null, we log + 200; gateway will retry within seconds.
2. Webhook signature key rotated by Tech Lead → old webhooks pile up signature-mismatch; need 24h overlap with both secrets supported. (Add as PW-9b if needed; out of scope this sprint — log only.)
3. Partial refund + another partial refund → `RefundedTotal` accumulates; final refund hits exact `AmountTotal` → Status = RefundedFull.
4. Refund amount = full original → Status = RefundedFull immediately (no partial intermediate).
5. Gateway returns `Pending` status on refund → leave Payment row as Pending; T5 RefundRetryService picks up Failed ones, not Pending — Pending awaits webhook.
6. User cancels booking with 0% refund (last-minute) → inbox handler skips creating Refund row, logs info; no money movement.
7. Provider cancels booking → ALWAYS 100% (Booking computes this; inbox handler trusts the payload).
8. Same gateway event delivered twice → idempotency via `_webhookInbox.HasBeenProcessedAsync` on `cmd.EventId`. Both succeed with 200.
9. Webhook for refund where original payment is not Completed (impossible from gateway but defensive) → log error, 200 to stop retries.
10. Currency drift between booking snapshot and original Payment → reject refund with `Refund.CurrencyMismatch`. Means the booking snapshot is stale; T2 handler logs critical for ops to investigate.

---

<a id="06-task-invoices-pdf"></a>

## 06-task-invoices-pdf

> Source: `Finance/06-task-invoices-pdf.md`

# TASK 3 — Invoices + PDF Generation

> **Owner:** Fadwa — **Hours:** 36h — **Hard deadline:** Sun **2026-09-27 17:00**
> **Earliest start:** Wed 2026-08-19 (parallel with T1; reads PaymentCompleted event)
> **Endpoints:** 3 + 1 domain event handler
> **Depends on:** T1 (Payment.MarkCompleted), PW (Invoice aggregate)

---

## 1. Endpoint Surface

| Method | Path | Permission | Returns |
|---|---|---|---|
| GET | `/api/v1/invoices/my-invoices` | `Invoice.Read` | Cursor envelope, self-filter |
| GET | `/api/v1/invoices/provider/my-invoices` | `Invoice.Read` | Self-filter by provider role |
| GET | `/api/v1/invoices/{id}` | `Invoice.Read` | Self/provider/admin ownership |
| GET | `/api/v1/invoices/{id}/download` | `Invoice.Download` | application/pdf stream |

**Domain event handler** (NOT HTTP):
- `OnPaymentCompletedGenerateInvoiceHandler` in `Finance.Infrastructure/EventHandlers/` — consumes `PaymentCompletedDomainEvent` → calls `Invoice.GenerateForPayment` factory + adds to DbContext + emits `InvoiceGeneratedDomainEvent`. INDEX §4 R15 — does NOT call SaveChangesAsync; the same UoW commit that wrote Payment completion writes the Invoice.

---

## 2. Invoice Aggregate Design

```csharp
public sealed class Invoice : AuditableEntity, IAggregateRoot
{
    public string InvoiceNumber { get; private set; }   // INV-{YYYYMM}-{seq6}
    public Guid PaymentId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Money AmountSubtotal { get; private set; }   // booking total
    public Money AmountTax { get; private set; }        // computed per region; this sprint stub = 0
    public Money AmountDiscount { get; private set; }   // sum of DiscountUsage; this sprint stub = 0
    public Money AmountTotal { get; private set; }      // == subtotal - discount + tax
    public InvoiceStatus Status { get; private set; }   // {Issued, Cancelled, Refunded}
    public DateTime IssuedAt { get; private set; }
    public string BuyerName { get; private set; }
    public string BuyerEmail { get; private set; }
    public string SellerName { get; private set; }      // provider business name
    public string SellerTaxId { get; private set; }
    public string? PdfStoragePath { get; private set; } // null until first download triggers render
    public ICollection<InvoiceItem> Items { get; private set; } = [];

    public static Invoice GenerateForPayment(
        Payment payment,
        BookingSnapshot booking,
        ProviderSnapshot provider,
        UserSnapshot buyer,
        IInvoiceNumberGenerator numberGen,
        TimeProvider timeProvider);

    public Result CancelDueToRefund(string reason);
    public Result MarkPdfRendered(string storagePath);
}
```

`InvoiceItem` (owned, BaseEntity):
```csharp
public sealed class InvoiceItem
{
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money Subtotal { get; private set; }
}
```

---

## 3. InvoiceNumber Generator (F-R8)

```csharp
public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(DateTime monthAnchorUtc, CancellationToken ct);
}
```

Implementation `SqlInvoiceNumberGenerator` (singleton):
- Stores month/sequence in `finance.InvoiceNumberSequence` table (PK = YYYYMM).
- Atomic increment via `UPDATE … SET seq = seq + 1 OUTPUT inserted.seq WHERE month = @m` (UPSERT pattern).
- Returns `$"INV-{YYYYMM}-{seq:D6}"`.

**Race safety:** SQL row-level lock during UPDATE. Concurrent webhooks safe because each gets a unique seq.

**Per-month sequence reset:** automatic — new month gets seq=1 because INSERT path creates the row.

---

## 4. PDF Generation (QuestPDF)

Add package: `QuestPDF` v2024.12+ to Finance.Infrastructure.csproj. **License flag:** QuestPDF Community license requires app config call `QuestPDF.Settings.License = LicenseType.Community;` in Program.cs (OK for YallaJo's annual revenue threshold; verify with Tech Lead).

**Template `InvoicePdfTemplate` in `Finance.Infrastructure/Pdf/`:**
- Header: YallaJo logo (from `/wwwroot/branding/logo.png`), "INVOICE", invoice number, issue date.
- Buyer block (right): name, email.
- Seller block (left): name, tax ID.
- Booking metadata table: tour name, date, group size, currency.
- Line items table: description / qty / unit price / subtotal.
- Totals: subtotal, discount, tax, total — bold, larger font.
- Footer: payment method, gateway txn ID (last 4 chars only — PCI), "Thank you for choosing YallaJo".

**Localization:** EN baseline. AR translation deferred (string keys ready but only EN populated). `Accept-Language: ar` will fall back to EN this sprint.

**Storage:** rendered PDFs go to a configurable blob path:
- Local dev: `App_Data/invoices/{userId}/{invoiceId}.pdf`.
- Prod: Azure Blob `invoices/{userId}/{invoiceId}.pdf` (binding via `IStorageProvider` already in SharedKernel.Infrastructure).
- Path stamped onto Invoice.PdfStoragePath on first render. Subsequent downloads stream from storage.

**Cache busting:** if Invoice is Cancelled / Refunded → PDF re-rendered next download (storage path nulled by `CancelDueToRefund`).

---

## 5. GET /invoices/{id}/download Flow

```csharp
public async Task<Result<InvoiceDownloadResponse>> Handle(DownloadInvoiceQuery q, CancellationToken ct)
{
    var invoice = await _invoiceRepo.GetByIdAsync(q.InvoiceId, ct);
    if (invoice is null) return Result.Failure<InvoiceDownloadResponse>(new Error("Invoice.NotFound", ""), Outcome.NotFound);

    // Ownership check
    if (!await OwnedByCallerAsync(invoice, ct))
        return Result.Failure<InvoiceDownloadResponse>(new Error("Invoice.OwnerMismatch", ""), Outcome.Forbidden);

    if (invoice.PdfStoragePath is null)
    {
        // Lazy render
        var pdfBytes = _pdfRenderer.Render(invoice);
        var path = await _storage.StoreAsync($"invoices/{invoice.UserId}/{invoice.Id}.pdf", pdfBytes, ct);
        invoice.MarkPdfRendered(path);
        await _uow.SaveChangesAsync(ct);
    }

    var stream = await _storage.OpenReadAsync(invoice.PdfStoragePath, ct);
    return Result.Success(new InvoiceDownloadResponse(stream, $"{invoice.InvoiceNumber}.pdf", "application/pdf"));
}
```

Endpoint returns `Results.Stream(stream, "application/pdf", invoice.InvoiceNumber + ".pdf")`.

**Performance:** first download <2 s, subsequent <300 ms (storage fetch only).

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `Invoice` aggregate + `InvoiceItem` owned + factory + tests | 5 | 2026-08-26 |
| 2 | Migration `FinanceAddInvoiceTable` + `IInvoiceNumberSequence` (1 table for seq counter) | 3 | 2026-08-27 |
| 3 | `SqlInvoiceNumberGenerator` + concurrency tests (100 parallel calls assert unique seqs) | 4 | 2026-08-31 |
| 4 | `OnPaymentCompletedGenerateInvoiceHandler` domain event handler + test | 3 | 2026-09-02 |
| 5 | QuestPDF package + license setup + `InvoicePdfTemplate` + visual review | 6 | 2026-09-07 |
| 6 | `IStorageProvider` wiring (local + Azure Blob backends) | 4 | 2026-09-09 |
| 7 | GET /my-invoices + GET /provider/my-invoices queries + handlers + cache | 4 | 2026-09-14 |
| 8 | GET /invoices/{id} + ownership guard | 2 | 2026-09-16 |
| 9 | GET /invoices/{id}/download endpoint + lazy render flow | 3 | 2026-09-20 |
| 10 | Unit tests for InvoiceNumberGenerator + Invoice factory invariants | 1 | 2026-09-23 |
| 11 | PR review + visual PDF QA | 1 | 2026-09-27 |
| **Total** | | **36h** | |

---

## 7. Edge cases

1. Two webhooks for same payment fire concurrently → `OnPaymentCompletedGenerateInvoiceHandler` is idempotent via `_invoiceRepo.GetByPaymentIdAsync` check at top; second call is a noop.
2. Booking refunded → `BookingTourBookingCancelledHandler` (in T2) raises domain event; T3 adds a sibling handler `OnBookingCancelledMarkInvoiceCancelledHandler` that calls `Invoice.CancelDueToRefund`. PDF re-rendered with watermark "CANCELLED" on next download.
3. Provider/User snapshot stale → invoice uses snapshot at moment of generation; subsequent provider name change does NOT rewrite issued invoices (audit purity).
4. QuestPDF license throws at runtime → log critical, return 500 with `Invoice.PdfRenderError`. Tech Lead alerted.
5. Storage write fails → don't mark invoice rendered; user gets 503 + retry-after. Next attempt succeeds.
6. Concurrent download of same not-yet-rendered invoice → unique-index `(InvoiceId, PdfStoragePath)` prevents double-stamp; loser uses winner's path next attempt.
7. Tax = 0 this sprint everywhere → display "Tax: JOD 0.00" not hidden (regulatory hygiene).
8. Invoice month boundary (12/31 23:59:59 UTC payment) → uses `IssuedAt` UTC date; month sequence comes from `IssuedAt` month.
9. InvoiceNumber sequence overflow (>999999/month) → unlikely; raise critical alert if >900K; expand to 7 digits in a v2 migration.

---

<a id="07-task-payouts"></a>

## 07-task-payouts

> Source: `Finance/07-task-payouts.md`

# TASK 4 — Payouts (Read, Trigger, Approve)

> **Owner:** Mohammad (LEAD) — **Hours:** 32h — **Hard deadline:** Sun **2026-10-04 17:00**
> **Earliest start:** Mon 2026-09-14
> **Endpoints:** 5
> **Depends on:** T1/T2 (Payment Completed), T6 (CommissionRule), Booking inbox events for escrow window

---

## 1. Endpoint Surface

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | GET | `/api/v1/payouts/admin/pending` | `Payout.Read` (admin) | All Status=Pending+Hold, sorted by oldest first |
| 2 | GET | `/api/v1/payouts/provider` | `Payout.Read` | Self-filter cursor |
| 3 | GET | `/api/v1/payouts/{id}` | `Payout.Read` | Self-filter / admin |
| 4 | POST | `/api/v1/payouts/admin/trigger` | `Payout.Trigger` | Manually fire `PayoutBatchingService` (normally weekly). Returns 202 + batch summary. |
| 5 | POST | `/api/v1/payouts/{id}/approve` | `Payout.Approve` | Required for payouts > 5000 currency-units |

Plus **3 inbox handlers:**
- `BookingTourBookingCompletedHandler` — sets `Payment.EscrowReleaseEligibleAt = CompletedAt + 7d` (F-R4)
- `AccountsProviderSubscriptionChangedHandler` — re-stamps `BookingProviderSnapshot.SubscriptionTier` (Booking module would also consume this; Finance keeps its own copy for commission lookup)
- `AccountsProviderBankAccountVerifiedHandler` — stub (logs only; deferred to Accounts sprint)

---

## 2. Payout Aggregate Design

```csharp
public sealed class Payout : AuditableEntity, IAggregateRoot
{
    public Guid ProviderId { get; private set; }
    public string Currency { get; private set; }
    public DateOnly BatchPeriodStart { get; private set; }
    public DateOnly BatchPeriodEnd { get; private set; }
    public Money GrossAmount { get; private set; }
    public Money CommissionAmount { get; private set; }
    public Money NetAmount { get; private set; }
    public PayoutStatus Status { get; private set; }   // {Pending, ReadyForPayout, Hold, Completed, Failed, ManuallyResolved}
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? GatewayPayoutId { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid? BankAccountId { get; private set; }   // ProviderBankAccount snapshot at time of payout
    public ICollection<PayoutItem> Items { get; private set; } = [];

    public static Payout CreateBatch(Guid providerId, string currency, DateOnly periodStart, DateOnly periodEnd, Guid? bankAccountId, TimeProvider tp);
    public Result AddItem(Guid bookingId, Money gross, Money commission, Guid? commissionRuleSnapshotId);
    public Result PutOnHold(string reason);
    public Result MarkReadyForPayout();
    public Result Approve(Guid approverUserId, TimeProvider tp);
    public Result MarkCompleted(string gatewayPayoutId, DateTime completedAtUtc);
    public Result MarkFailed(string reason);
}
```

`PayoutItem` (owned, BaseEntity):
```csharp
public sealed class PayoutItem
{
    public Guid BookingId { get; private set; }
    public Money GrossAmount { get; private set; }
    public Money CommissionAmount { get; private set; }
    public Money NetAmount { get; private set; }
    public Guid? CommissionRuleSnapshotId { get; private set; }  // audit traceability
}
```

---

## 3. POST /payouts/admin/trigger Flow

This is a **manual** trigger; same logic as `PayoutBatchingService` (T5) but synchronous and returns a summary.

```csharp
public async Task<Result<TriggerPayoutResponse>> Handle(TriggerPayoutCommand cmd, CancellationToken ct)
{
    var anchor = _timeProvider.GetUtcNow().UtcDateTime;
    var period = (Start: DateOnly.FromDateTime(anchor.AddDays(-7)), End: DateOnly.FromDateTime(anchor));

    // 1. Pull eligible payments
    var eligible = await _paymentRepo.GetEscrowReleaseEligibleAsync(period.End, ct);
    if (eligible.Count == 0) return Result.Success(new TriggerPayoutResponse(Created: 0, Skipped: 0, OnHold: 0));

    // 2. Group by (Provider, Currency)
    var groups = eligible.GroupBy(p => new { p.ProviderId, Currency = p.AmountTotal.Currency });

    int created = 0, skipped = 0, onHold = 0;
    foreach (var group in groups)
    {
        // 3. Look up provider's verified bank account for this currency
        var bankAcct = await _bankRepo.GetVerifiedForProviderAsync(group.Key.ProviderId, group.Key.Currency, ct);

        // 4. Look up commission rule (F-R6)
        var commissionRule = await _commissionLookup.GetCommissionForProviderAsync(group.Key.ProviderId, group.Key.Currency, ct);

        var payout = Payout.CreateBatch(
            providerId: group.Key.ProviderId,
            currency: group.Key.Currency,
            periodStart: period.Start,
            periodEnd: period.End,
            bankAccountId: bankAcct?.Id,
            tp: _timeProvider);

        var disputedPaymentIds = await _disputeLookup.GetDisputedPaymentIdsAsync(group.Select(p => p.Id), ct); // Phase 3 stub returns []

        foreach (var payment in group)
        {
            if (disputedPaymentIds.Contains(payment.Id))
            {
                // hold logic — payout has disputed item, can't release fully
                payout.PutOnHold("Has disputed payment items");
                onHold++;
                break;
            }

            var gross = payment.AmountTotal.Amount - payment.RefundedTotal;
            if (gross <= 0) continue;  // fully refunded
            var commission = Math.Round(gross * (commissionRule.Percentage / 100m), 2, MidpointRounding.ToEven);
            var net = gross - commission;
            payout.AddItem(payment.BookingId,
                new Money(gross, group.Key.Currency),
                new Money(commission, group.Key.Currency),
                commissionRuleSnapshotId: commissionRule.SnapshotId);
        }

        // 5. Min threshold check (F-R7)
        if (payout.NetAmount.Amount < _options.MinPayoutThreshold(group.Key.Currency))
        {
            skipped++;
            continue;  // do not save; rolls into next batch
        }

        // 6. Bank account presence
        if (bankAcct is null)
        {
            payout.PutOnHold("No verified bank account");
            onHold++;
        }
        else if (payout.NetAmount.Amount > _options.LargePayoutThreshold(group.Key.Currency))
        {
            // Stay Pending — needs admin approve
        }
        else
        {
            payout.MarkReadyForPayout();
        }

        await _payoutRepo.AddAsync(payout, ct);
        created++;

        // Raise integration event for Messaging notification
        // Outbox row `finance.payout.scheduled.v1`
    }

    await _uow.SaveChangesAsync(ct);
    return Result.Success(new TriggerPayoutResponse(created, skipped, onHold));
}
```

`PayoutCreatedDomainEvent` raised inside `Payout.CreateBatch`; outbox writes `finance.payout.scheduled.v1`.

---

## 4. POST /payouts/{id}/approve Flow

```csharp
public async Task<Result> Handle(ApprovePayoutCommand cmd, CancellationToken ct)
{
    var payout = await _payoutRepo.GetByIdAsync(cmd.PayoutId, ct);
    if (payout is null) return Result.Failure(new Error("Payout.NotFound", ""), Outcome.NotFound);
    if (payout.Status is not PayoutStatus.Pending)
        return Result.Failure(new Error("Payout.NotPending", $"Status was {payout.Status}"), Outcome.Conflict);

    var approveResult = payout.Approve(_currentUser.UserId, _timeProvider);
    if (approveResult.IsFailure) return approveResult;
    // Raises PayoutApprovedDomainEvent

    // Trigger gateway call now (small payouts trigger themselves in T5; large ones wait for approve)
    try
    {
        var gw = await _gateway.PayoutAsync(new PayoutRequest(payout.Id, payout.BankAccountId!.Value.ToString(), payout.NetAmount.Amount, payout.NetAmount.Currency), ct);
        if (gw.Status == PayoutGatewayStatus.Completed)
            payout.MarkCompleted(gw.GatewayPayoutId, _timeProvider.GetUtcNow().UtcDateTime);
        else if (gw.Status == PayoutGatewayStatus.Pending) { /* leave Pending; webhook brings it forward */ }
        else
            payout.MarkFailed(gw.FailureCode ?? "Unknown");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Gateway payout failed for {Id}", payout.Id);
        payout.MarkFailed("GatewayError");
    }

    await _uow.SaveChangesAsync(ct);
    return Result.Success();
}
```

`PayoutCompletedDomainEvent` raised on success → `finance.payout.completed.v1` outbox → Messaging notifies provider.

---

## 5. Inbox Handlers — quick spec

**`BookingTourBookingCompletedHandler`:**
```csharp
public async Task Handle(IntegrationEventEnvelope<BookingTourBookingCompletedIntegrationEvent> e, CancellationToken ct)
{
    if (await _inbox.HasBeenProcessedAsync(e.EventId, ct)) return;
    var ev = e.Payload;
    var payment = await _paymentRepo.GetActiveByBookingIdAsync(ev.BookingId, ct);
    if (payment is null) { _logger.LogWarning("No payment for completed booking"); _inbox.MarkAsProcessed(e.EventId); await _uow.SaveChangesAsync(ct); return; }
    payment.StampEscrowReleaseEligibleAt(ev.CompletedAt.AddDays(_options.EscrowReleaseDays));  // default 7d
    _inbox.MarkAsProcessed(e.EventId);
    await _uow.SaveChangesAsync(ct);
}
```

**`AccountsProviderSubscriptionChangedHandler`:** updates `BookingProviderSnapshot.SubscriptionTier` in Finance's local snapshot table for commission lookup.

**`AccountsProviderBankAccountVerifiedHandler`:** stub — logs `[INFO] Bank account verified for provider {Id}`. Real flow ships with Accounts module sprint.

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `Payout` aggregate + `PayoutItem` owned + state machine methods + tests | 6 | 2026-09-17 |
| 2 | Migration `FinanceAddPayoutAndPayoutItems` + indexes | 2 | 2026-09-18 |
| 3 | `IPayoutOptions` (MinPayoutThreshold per currency, LargePayoutThreshold, EscrowReleaseDays) | 1 | 2026-09-18 |
| 4 | `ICommissionLookupService` real impl in Finance.Infrastructure (THIS sprint, also Booking uses snapshot) | 3 | 2026-09-21 |
| 5 | POST /admin/trigger handler + query + validator + permission | 6 | 2026-09-24 |
| 6 | POST /{id}/approve handler + gateway call + tests | 4 | 2026-09-27 |
| 7 | 3 GET endpoints + queries + cache | 4 | 2026-09-29 |
| 8 | 3 inbox handlers + tests | 3 | 2026-10-01 |
| 9 | Integration test: 3 completed bookings → trigger → 1 Payout w/ 3 items → approve → gateway call → Completed | 2 | 2026-10-03 |
| 10 | PR review | 1 | 2026-10-04 |
| **Total** | | **32h** | |

---

## 7. Edge cases

1. Provider has 0 eligible payments → no Payout row created; not an error.
2. Provider's only bank account became Unverified between batches → Payout PutOnHold; admin manually resolves.
3. Dispute filed AFTER payout already created (rare race) → Payout still proceeds (escrow already released); dispute resolution recovers from "platform reserve" then deducts from next payout (Phase 3 dispute module owns this).
4. `EscrowReleaseDays` config changed mid-sprint → only applies to FUTURE PaymentCompleted events; existing payments retain their original eligible date.
5. Approve called on Hold payout → 409 `Payout.NotPending`; admin must unhold first via separate admin endpoint (DEFERRED — log issue, manual SQL for v1).
6. Gateway webhook for payout (separate from payment webhook) → Phase 3 — out of scope. Manual reconciliation via admin endpoint POST /payouts/{id}/mark-completed (deferred but documented).
7. Commission rule deleted between trigger and approve → snapshot saved at AddItem time; deletion does NOT mutate existing payouts.
8. `MinPayoutThreshold` updated mid-run → uses value at start of batch; consistent across all groups in same batch.
9. Provider has multiple verified bank accounts for same currency → uses `GetVerifiedForProviderAsync` default (smallest CreatedAt or explicitly marked IsPrimary in ProviderBankAccount entity).

---

<a id="08-task-background-services"></a>

## 08-task-background-services

> Source: `Finance/08-task-background-services.md`

# TASK 5 — Background Services (PayoutBatching + RefundRetry)

> **Owner:** Mohammad — **Hours:** 28h — **Hard deadline:** Sun **2026-10-11 17:00**
> **Earliest start:** Mon 2026-10-05 (after T4)
> **Endpoints:** 0 HTTP. **2 BackgroundService implementations** + (optional bonus) admin manual-trigger endpoints (already done via T4's POST /admin/trigger for batching).
> **Depends on:** T4 (payout state machine), T2 (refund creation flow), Booking 10-task pattern reference

Follows the shared pattern documented in `../Booking/10-task-background-services.md §1`. Uses `BackgroundService` + `PeriodicTimer` + `IServiceProvider.CreateScope` per ADR-003.

---

## 1. PayoutBatchingService (weekly Sun midnight UTC)

**Goal:** automate `POST /payouts/admin/trigger` flow on a cron schedule.

```csharp
internal sealed class PayoutBatchingService(
    IServiceProvider sp,
    ILogger<PayoutBatchingService> logger,
    TimeProvider tp,
    IOptions<PayoutBatchingOptions> opts)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opts.Value.Enabled) { logger.LogInformation("PayoutBatchingService disabled"); return; }
        logger.LogInformation("PayoutBatchingService started, scheduling for Sunday midnight UTC");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = NextWeekly(tp.GetUtcNow().UtcDateTime, opts.Value.TargetDayOfWeek, opts.Value.TargetTimeUtc);
            var wait = nextRun - tp.GetUtcNow().UtcDateTime;
            logger.LogInformation("Next payout batch at {Next:o} (in {Hours:0.0}h)", nextRun, wait.TotalHours);
            try { await Task.Delay(wait, stoppingToken); }
            catch (OperationCanceledException) { return; }

            using var scope = sp.CreateScope();
            try
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new TriggerPayoutCommand(IsManual: false), stoppingToken);
                if (result.IsSuccess)
                    logger.LogInformation("Weekly batch: created={C} skipped={S} onHold={H}", result.Value.Created, result.Value.Skipped, result.Value.OnHold);
                else
                    logger.LogError("Weekly batch failed: {Code} {Message}", result.Error?.Code, result.Error?.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogError(ex, "PayoutBatchingService tick failed");
                FinanceDiagnostics.BgServiceFailures.Add(1, new("service", nameof(PayoutBatchingService)));
            }
        }
    }

    static DateTime NextWeekly(DateTime nowUtc, DayOfWeek targetDay, TimeOnly targetTime)
    {
        var diff = ((int)targetDay - (int)nowUtc.DayOfWeek + 7) % 7;
        var candidate = nowUtc.Date.AddDays(diff).Add(targetTime.ToTimeSpan());
        if (candidate <= nowUtc) candidate = candidate.AddDays(7);
        return candidate;
    }
}

public sealed class PayoutBatchingOptions
{
    public bool Enabled { get; set; } = true;
    public DayOfWeek TargetDayOfWeek { get; set; } = DayOfWeek.Sunday;
    public TimeOnly TargetTimeUtc { get; set; } = new(0, 0);  // midnight UTC
}
```

**Why we use MediatR.Send instead of duplicating handler code:** keeps the trigger flow DRY — admin manual trigger (T4) and BG service walk the same code path. Auditing identical.

**Edge cases:**
1. Service started Tuesday 14:00 → first run = next Sunday midnight (5 days away). Logged at startup so ops sees.
2. Multi-instance deployment → competing schedulers. Solution: use SQL-based distributed lock (`SELECT … FROM finance.LeaderElection WITH (UPDLOCK, READPAST) WHERE Key='PayoutBatching' AND ExpiresAt > now`). DEFERRED — single-instance acceptable v1. Document in 10-cross-cutting.md.
3. Tick takes > 1 hour → next tick still computes from current time, will still align with following Sunday.
4. Daylight saving / TZ confusion → never. Everything UTC.

---

## 2. RefundRetryService (every 15 min)

**Goal:** pick up Refund-type Payment rows with Status=Failed AND RetryCount<3 and retry the gateway call.

```csharp
internal sealed class RefundRetryService(
    IServiceProvider sp,
    ILogger<RefundRetryService> logger,
    TimeProvider tp,
    IOptions<RefundRetryOptions> opts)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opts.Value.Enabled) return;
        using var timer = new PeriodicTimer(opts.Value.Interval);
        await Task.Delay(opts.Value.InitialDelay, stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = sp.CreateScope();
            try { await ProcessBatchAsync(scope.ServiceProvider, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogError(ex, "RefundRetryService tick failed");
                FinanceDiagnostics.BgServiceFailures.Add(1, new("service", nameof(RefundRetryService)));
            }
        }
    }

    private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var paymentRepo = scoped.GetRequiredService<IPaymentRepository>();
        var gateway     = scoped.GetRequiredService<IPaymentGateway>();
        var uow         = scoped.GetRequiredService<IFinanceUnitOfWork>();

        var pending = await paymentRepo.GetPendingRefundsOlderThanAsync(
            cutoff: tp.GetUtcNow().UtcDateTime.AddMinutes(-opts.Value.RetryCooloffMinutes),
            maxRetries: opts.Value.MaxRetries,
            batchSize: opts.Value.BatchSize,
            ct);

        foreach (var refund in pending)
        {
            var original = await paymentRepo.GetByIdAsync(refund.OriginalPaymentId!.Value, ct);
            if (original is null) continue;

            try
            {
                var gw = await gateway.RefundAsync(
                    new RefundRequest(original.GatewayTransactionId, refund.AmountTotal.Amount, refund.AmountTotal.Currency, refund.RefundReason!.Value.ToString()),
                    ct);

                refund.IncrementRetryCount();
                if (gw.Status == RefundStatus.Completed) refund.MarkRefundCompleted(gw.GatewayRefundId, tp.GetUtcNow().UtcDateTime);
                else if (gw.Status == RefundStatus.Failed)
                {
                    refund.MarkRefundFailed(gw.FailureCode ?? "Unknown");
                    if (refund.RetryCount >= opts.Value.MaxRetries)
                    {
                        // Final failure — emit alert event
                        // RefundFailedDomainEvent → finance.refund.failed.v1 → Messaging admin alert
                        refund.RaiseRefundFinallyFailed();
                    }
                }
                // Pending stays Pending; webhook will catch up
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refund retry gateway error for {Id}", refund.Id);
                refund.IncrementRetryCount();
                refund.MarkRefundFailed("GatewayError");
            }
        }
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class RefundRetryOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(2);
    public int RetryCooloffMinutes { get; set; } = 5;  // don't retry the same row again within 5 min of last attempt
    public int MaxRetries { get; set; } = 3;
    public int BatchSize { get; set; } = 200;
}
```

**`Payment.IncrementRetryCount` / `RaiseRefundFinallyFailed` domain methods** added in this task.

**Inbox parity** — none needed. RefundRetryService is fully internal.

**Edge cases:**
1. Same refund row retried > cooloff window before next tick → cooloff guard prevents thrashing. Cutoff = "older than X minutes since LastModified".
2. Gateway slow (12 sec per call) × 200 batch = 40 min per tick → batch size smaller in prod (e.g. 50). Configurable.
3. After 3 failures + RefundFinallyFailed event → Messaging admin alert; ops manually reconciles via Stripe dashboard + admin endpoint POST /admin/payments/{id}/mark-refund-completed (deferred — manual SQL for v1).

---

## 3. DI Registration

```csharp
public static IServiceCollection AddFinanceInfrastructure(this IServiceCollection services, IConfiguration cfg)
{
    // ... DbContext, repos, UoW, inbox, outbox, gateway, ICommissionLookupService impl ...

    services.Configure<PayoutBatchingOptions>(cfg.GetSection("Finance:BackgroundServices:PayoutBatching"));
    services.Configure<RefundRetryOptions>(cfg.GetSection("Finance:BackgroundServices:RefundRetry"));
    services.AddHostedService<RefundRetryService>();
    services.AddHostedService<PayoutBatchingService>();
    return services;
}
```

`appsettings.json`:
```json
"Finance": {
  "BackgroundServices": {
    "PayoutBatching": { "Enabled": true, "TargetDayOfWeek": "Sunday", "TargetTimeUtc": "00:00:00" },
    "RefundRetry":    { "Enabled": true, "Interval": "00:15:00", "InitialDelay": "00:02:00", "RetryCooloffMinutes": 5, "MaxRetries": 3, "BatchSize": 200 }
  },
  "Payout": {
    "MinPayoutThreshold": { "JOD": 10, "USD": 15, "EUR": 12 },
    "LargePayoutThreshold": { "JOD": 5000, "USD": 7000, "EUR": 6500 },
    "EscrowReleaseDays": 7
  }
}
```

Dev overrides: `PayoutBatching.Interval = 00:10:00` (run every 10 min — fast feedback), `RefundRetry.RetryCooloffMinutes = 1`.

---

## 4. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Options classes + appsettings + dev overrides | 2 | 2026-10-06 |
| 2 | `PayoutBatchingService` impl + `NextWeekly` helper + unit test | 5 | 2026-10-07 |
| 3 | `RefundRetryService` impl + cooloff guard + unit test | 5 | 2026-10-07 |
| 4 | `Payment.IncrementRetryCount` + `RaiseRefundFinallyFailed` domain methods | 2 | 2026-10-08 |
| 5 | `FinanceDiagnostics` static class (ActivitySource + Meter + counters) | 2 | 2026-10-08 |
| 6 | DI registration + health check + boot log | 1 | 2026-10-08 |
| 7 | Integration test: completed payment + 7d escrow + run trigger → 1 Payout row | 3 | 2026-10-09 |
| 8 | Integration test: refund failed 3× → finally-failed event in outbox | 2 | 2026-10-10 |
| 9 | 24h soak test in pre-prod + verify OTEL counters | 3 | 2026-10-11 |
| 10 | PR review fixes | 3 | 2026-10-11 |
| **Total** | | **28h** | |

---

## 5. Acceptance gate for T5

- [ ] Boot log shows both services started with their schedule.
- [ ] OTEL counters incrementing: `bg_service_ticks_total{service=RefundRetryService}` ≈ 96 in 24h; `…{service=PayoutBatchingService}` = 0 or 1 (weekly).
- [ ] Smoke test: insert refund row with `Status=Failed, RetryCount=0, AmountTotal=10 JOD`, wait 15 min, query → `RetryCount=1, Status=Completed`.
- [ ] Smoke test (pre-prod): manually advance system date to Sunday 00:00 (or override `TargetTimeUtc` to next minute) → run service → assert 1 PayoutBatchCreated event in `finance.OutboxMessages`.
- [ ] Multi-instance test (if any): two API instances run; only ONE schedules a tick at midnight (lock contention — Tech Lead applies distributed lock if v2 sprint requires multi-instance).

---

<a id="09-task-commission-rules"></a>

## 09-task-commission-rules

> Source: `Finance/09-task-commission-rules.md`

# TASK 6 — Commission Rules CRUD

> **Owner:** Junior Dev (TBD) or Fadwa fallback — **Hours:** 24h — **Hard deadline:** Sun **2026-09-27 17:00**
> **Earliest start:** Wed 2026-08-19 (parallel — Booking module will start consuming `finance.commission-rule.*` events when this ships)
> **Endpoints:** 3
> **Depends on:** PW only

This task ships the admin-only commission-rule CRUD that Booking module consumes via the `BookingCommissionSnapshot` inbox handler documented in Booking 05-task.

---

## 1. Endpoint Surface

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | GET | `/api/v1/commissions` | `CommissionRule.Read` (admin) | Paginated list, filter `tier`, `currency`, `revenueRange` |
| 2 | POST | `/api/v1/commissions` | `CommissionRule.Create` | Admin upserts a rule |
| 3 | PUT | `/api/v1/commissions/{id}` | `CommissionRule.Update` | Admin edits |
| 4 | DELETE | `/api/v1/commissions/{id}` | `CommissionRule.Delete` | Soft delete + emits `finance.commission-rule.deleted.v1` |

(GET is intentionally a single endpoint; 4 total. The master INDEX counts the four endpoints in the same row as "Commission Rules CRUD" = 3 because we group the GET into one row of the manifest.)

---

## 2. CommissionRule Aggregate

```csharp
public sealed class CommissionRule : AuditableEntity, IAggregateRoot
{
    public string Tier { get; private set; }              // "Free", "Basic", "Premium", "Enterprise" — free text v1
    public decimal MinMonthlyRevenue { get; private set; } // inclusive lower bound
    public decimal? MaxMonthlyRevenue { get; private set; } // null = open-ended top tier
    public string Currency { get; private set; }           // ISO 3-letter
    public decimal Percentage { get; private set; }        // 0 < p < 100
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    public static CommissionRule Create(string tier, decimal minMonthlyRevenue, decimal? maxMonthlyRevenue, string currency, decimal percentage, string? notes, TimeProvider tp);
    public Result Update(decimal minMonthlyRevenue, decimal? maxMonthlyRevenue, decimal percentage, string? notes);
    public Result Deactivate(string reason);
}
```

Factory raises `CommissionRuleUpsertedDomainEvent`; `Update` raises it again; `Deactivate` raises `CommissionRuleDeletedDomainEvent`.

---

## 3. Validation Rules

- `Tier` not empty, max 50 chars
- `Currency` in {JOD, USD, EUR}
- `MinMonthlyRevenue >= 0`
- `MaxMonthlyRevenue is null OR MaxMonthlyRevenue > MinMonthlyRevenue`
- `0 < Percentage < 100`
- **Overlap detection** (handler-level): for the same `(Tier, Currency)`, no two rules can have overlapping `[Min, Max]` ranges. Error code `CommissionRule.OverlapTier`.

---

## 4. Handler Flow — POST /commissions

```csharp
public async Task<Result<Guid>> Handle(UpsertCommissionRuleCommand cmd, CancellationToken ct)
{
    // Overlap check
    var overlaps = await _ruleRepo.GetOverlappingAsync(cmd.Tier, cmd.Currency, cmd.MinMonthlyRevenue, cmd.MaxMonthlyRevenue, excludeId: null, ct);
    if (overlaps.Count > 0)
        return Result.Failure<Guid>(new Error("CommissionRule.OverlapTier", $"Overlaps with rule {overlaps[0].Id}"), Outcome.Conflict);

    var rule = CommissionRule.Create(cmd.Tier, cmd.MinMonthlyRevenue, cmd.MaxMonthlyRevenue, cmd.Currency, cmd.Percentage, cmd.Notes, _timeProvider);
    await _ruleRepo.AddAsync(rule, ct);
    await _uow.SaveChangesAsync(ct);
    await _cache.RemoveByTagAsync("commission-rules", ct);
    // Outbox emits `finance.commission-rule.upserted.v1` → Booking inbox refreshes BookingCommissionSnapshot
    return Result.Success(rule.Id);
}
```

PUT does the same minus `Create`; uses `Update` method; raises `CommissionRuleUpsertedDomainEvent` again (Booking inbox idempotency handles).

DELETE calls `Deactivate("admin deleted")` → `CommissionRuleDeletedDomainEvent` → outbox `finance.commission-rule.deleted.v1` → Booking removes snapshot.

**Soft delete preserves history** for audit; `IsActive=false` rows excluded from GET unless `?includeInactive=true`.

---

## 5. Default Seed Rules (DataSeeder for clean envs)

`Finance.Infrastructure/Persistence/FinanceDbInitializer.cs` seeds these on fresh DB or empty CommissionRules table:

| Tier | MinRev | MaxRev | Currency | % | Notes |
|---|---|---|---|---|---|
| Free | 0 | null | JOD | 15.00 | Default free tier (PDF 2 §1.5) |
| Free | 0 | null | USD | 15.00 | |
| Free | 0 | null | EUR | 15.00 | |
| Basic | 0 | null | JOD | 10.00 | Reserved for Phase 3 subscription |
| Premium | 0 | null | JOD | 7.00 | Reserved for Phase 3 subscription |

Seeded only on env `Development` and `Staging` — production seed is admin-controlled.

---

## 6. Cache & Tags

| Cache | Key | TTL | Tag |
|---|---|---|---|
| All rules list | `commission-rules:list:{filterHash}` | 5 min | `commission-rules` |
| Single rule | `commission-rule:{id}` | 10 min | `commission-rules`, `commission-rule:{id}` |

Any POST/PUT/DELETE invalidates `commission-rules` tag.

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `CommissionRule` aggregate + tests | 3 | 2026-08-26 |
| 2 | Migration `FinanceAddCommissionRuleConstraints` (unique index, soft-delete filter) | 1 | 2026-08-27 |
| 3 | `ICommissionRuleRepository` + impl + overlap finder | 3 | 2026-08-31 |
| 4 | Default seed in DbInitializer | 1 | 2026-08-31 |
| 5 | POST endpoint + handler + validator | 4 | 2026-09-03 |
| 6 | PUT endpoint + handler + validator | 3 | 2026-09-07 |
| 7 | DELETE endpoint + handler + soft-delete behavior | 2 | 2026-09-09 |
| 8 | GET (list) endpoint + filter + cache + tag | 3 | 2026-09-14 |
| 9 | Unit tests for overlap detection edge cases | 2 | 2026-09-21 |
| 10 | Integration test: POST commission → outbox row → Booking inbox stub consumes | 1 | 2026-09-23 |
| 11 | PR review | 1 | 2026-09-27 |
| **Total** | | **24h** | |

---

## 8. Edge cases

1. Tier renamed (e.g. "Premium" → "Pro") — out of scope; PUT only changes numeric fields. Use DELETE + POST to rename.
2. New tier introduced (e.g. "Enterprise") with no booking yet → no Booking inbox row, just empty snapshot category; safe.
3. Overlap detection includes the rule itself on PUT → exclude `cmd.Id`.
4. Open-ended top tier (MaxMonthlyRevenue=null) → considered as "Min, +∞"; allowed at most ONE such rule per (Tier, Currency).
5. Currency added later (e.g. SAR) → CommissionRule.Currency validator includes only JOD/USD/EUR this sprint; expansion is future migration.
6. Percentage of 0 → blocked (must be > 0).
7. Booking sprint's BookingCommissionSnapshot consumer fails repeatedly → outbox row stays unprocessed; eventually retries — Booking ops responsibility.

---

<a id="10-cross-cutting"></a>

## 10-cross-cutting

> Source: `Finance/10-cross-cutting.md`

# Finance — Cross-Cutting Concerns

> Mirrors Booking 11-cross-cutting.md. Tech Lead enforces during PR review.

---

## 1. DI Audit

`Finance.Infrastructure/DependencyInjection.cs` MUST register:

| Registration | Lifetime | Notes |
|---|---|---|
| `IDbContextFactory<FinanceDbContext>` + pooled `FinanceDbContext` | Singleton + Scoped | Same pattern as Booking |
| `IFinanceUnitOfWork → FinanceUnitOfWork` | Scoped | Delegates to `IUnitOfWork<FinanceDbContext>` (PW-1) |
| `IFinanceInboxStore → FinanceInboxStore` | Scoped | Idempotency for integration events |
| `IFinanceOutboxWriter → FinanceOutboxWriter` | Scoped | |
| `IPaymentRepository, IPayoutRepository, IPayoutItemRepository, IInvoiceRepository, ICommissionRuleRepository, IProviderBankAccountRepository, IPaymentExpectationRepository, IInvoiceNumberGenerator` | Scoped (last one Singleton) | |
| `IPaymentGateway → FakePaymentGateway` (dev/test) OR `StripeGateway` etc. | Singleton | Switch by config `Finance:Gateway:Provider` |
| `ICommissionLookupService → CommissionLookupService` | Scoped | This is the IMPL for Booking module's stub (PW-6 placeholder). Booking module's DI continues to register a stub for itself; Finance's impl is used when the host is the same process — see "Cross-module wiring" below |
| `IDiscountEvaluator → NullDiscountEvaluator` | Singleton | Phase 4 ships real impl |
| `IStorageProvider → AzureBlobStorageProvider` (prod) / `LocalFileStorageProvider` (dev) | Singleton | T3 invoice PDFs |
| `IInvoicePdfRenderer → QuestPdfInvoiceRenderer` | Singleton | T3 |
| `IFinanceCacheKeys` | Singleton | |
| `IPermissionCatalog → FinancePermissionCatalog` | Singleton | |
| 2 BackgroundService: `PayoutBatchingService`, `RefundRetryService` | Singleton | `AddHostedService<T>` |
| MediatR Application assembly | per-call | |
| FluentValidators | Scoped | `includeInternalTypes: true` |

### Cross-module wiring note

Booking module's `BookingInfrastructure.AddBookingInfrastructure` registered a **stub** `CommissionLookupService` that reads `BookingCommissionSnapshot`. Finance ALSO registers its own `ICommissionLookupService` — but they have different consumer scopes:

- **Booking handlers** resolve `ICommissionLookupService` from THEIR module DI scope → gets the snapshot-reader (no cross-DB-call).
- **Finance handlers** resolve `ICommissionLookupService` from THEIR module DI scope → gets the authoritative reader (hits Finance DB).

This works because MediatR uses scoped DI per request. To prevent the cross-wiring from registering twice into the SAME container (the host registers both modules), Tech Lead audits that the interface is registered with `services.AddScoped<ICommissionLookupService>(sp => { /* current module context */ })` using a custom scope key. Alternative (simpler): rename Booking's stub to `IBookingCommissionLookupService` and the cross-module contract from Finance to `ICommissionLookupService`. Decision: **rename in PW-6 of Finance sprint** — Booking sprint's stub becomes Booking-local.

### Common mistakes (fail-PR triggers)

- Registering `FinanceDbContext` Scoped AND `AddDbContextPool` (duplicate).
- Forgetting `IPermissionCatalog` registration → silent permission gap.
- Registering `FakePaymentGateway` in production env (check `IHostEnvironment.IsProduction()`).
- Registering MediatR with wrong assembly (use Application marker, not Program).

---

## 2. Permission Seeder Verification

Expected boot log after this sprint merges:
```
[INFO] PermissionSeeder discovered 8 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance
[INFO] PermissionSeeder inserted/verified 22 Finance permissions
```

Run `SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Finance.%'` → expect **22**.

---

## 3. Outbox Type-Registry Validation

Add to `tests/Finance.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs` (mirrors Booking pattern).

**10 logical names** expected for Finance (all listed in 03-entities-matrix.md §4).

**Inbox consumers** (5 names from cross-module events):
- `booking.tour-booking.created.v1`
- `booking.tour-booking.completed.v1`
- `booking.tour-booking.cancelled.v1`
- `accounts.provider.subscription-changed.v1`
- `accounts.provider-bank-account.verified.v1` (stub handler, deferred)

---

## 4. Build Lock Workaround

Same as Booking — stop `YallaJo.Web` instances; build only Finance projects:
```powershell
dotnet build Finance/Finance.Domain/Finance.Domain.csproj
dotnet build Finance/Finance.Contracts/Finance.Contracts.csproj
dotnet build Finance/Finance.Application/Finance.Application.csproj
dotnet build Finance/Finance.Infrastructure/Finance.Infrastructure.csproj
dotnet build Finance/Finance.Presentation/Finance.Presentation.csproj
dotnet build tests/Finance.Tests.Unit/Finance.Tests.Unit.csproj
dotnet build tests/Finance.IntegrationTests/Finance.IntegrationTests.csproj
```

---

## 5. Migration Sequence

| # | Name | Owner | Task |
|---|---|---|---|
| 1 | `FinanceAddAggregateRootAndAuditMembers` | Tech Lead | PW-2 |
| 2 | `FinanceCreatePaymentIndexes` | Mahmoud | T1 |
| 3 | `FinanceAddInvoiceTable` | Fadwa | T3 |
| 4 | `FinanceAddPayoutAndPayoutItems` | Mohammad | T4 |
| 5 | `FinanceAddCommissionRuleConstraints` | Junior/Fadwa | T6 |
| 6 | `FinanceAddAuditLogTable` | Mahmoud | T1/T2 |

Tech Lead applies in shared environments. Devs only run `dotnet ef migrations add` locally.

---

## 6. Inbox / Outbox Hygiene

Existing `CompositeOutboxProcessor` auto-picks up `FinanceDbContext` when registered. Verify boot log:
```
[INFO] CompositeOutboxProcessor monitoring N DbContexts: ..., FinanceDbContext
```

Alerts (Grafana / Seq):
- `finance.OutboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now-5min` > 100 → page on-call
- `finance.InboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now-5min` > 100 → page on-call
- `finance.OutboxMessages WHERE LogicalName = 'finance.refund.failed.v1'` ANY > 0 → page on-call (refund of last resort failed for someone)

Cleanup: `OutboxCleaner` retains processed rows 7 days; `InboxCleaner` 30 days. No Finance-specific config.

---

## 7. PCI Compliance Recheck (Tech Lead end-of-sprint audit)

Run `tests/Finance.IntegrationTests/Compliance/PciAuditTests.cs`:
- Assert no test contains a real-looking PAN string (16-digit).
- Assert `Payment.GatewayTransactionId` column is the ONLY identifier of the transaction (no PAN reference fields).
- Assert webhook handler rejects requests where signature header missing (returns 400 not 401 — ensures no info leak).
- Assert log files in `logs/finance-*.json` don't contain `"cardNumber"`, `"cvv"`, `"expiry"` substrings.

---

## 8. Secrets Inventory

These MUST be in environment variables (NOT appsettings.json):

| Key | Source | Used by |
|---|---|---|
| `Finance__Gateway__Provider` | env | DI selection |
| `Finance__Gateway__ApiKey` | env (KeyVault prod) | `StripeGateway` etc. |
| `Finance__Gateway__WebhookSecret` | env (KeyVault prod) | T2 webhook HMAC |
| `Finance__Storage__AzureBlobConnectionString` | env (KeyVault prod) | T3 InvoicePdf store |
| `Finance__Pdf__QuestPdfLicense` | env | T3 PDF render |

Local dev `appsettings.Development.json` may include `"Provider": "Fake"` + dummy webhook secret — but NO real secrets ever committed.

---

<a id="99-acceptance-gate"></a>

## 99-acceptance-gate

> Source: `Finance/99-acceptance-gate.md`

# Finance Module — Final Acceptance Gate

> **Tech Lead signs off before declaring the Finance sprint closed (Thu 2026-10-15 17:00).**

---

## 1. Code Quality
- [ ] All 6 task PRs (T1..T6) merged.
- [ ] `dotnet build` green for every Finance project + 2 test projects.
- [ ] No new TODOs in `Finance/` outside `# TODO Phase 3` markers.
- [ ] All command handlers inject `ILogger<THandler>` + invalidate `RemoveByTagAsync` after SaveChanges.
- [ ] All queries implement `ICacheableQuery`.
- [ ] No bare `RequireAuthorization()` — `rg "RequireAuthorization\(\)\s*$" Finance/Finance.Presentation/` returns 0.
- [ ] Webhook endpoint uses `.AllowAnonymous()` (verified by `rg ".AllowAnonymous()" Finance/Finance.Presentation/Webhook` returns 1).
- [ ] No `ICurrentUser` in handlers EXCEPT documented ownership comparisons (02-critical-rules.md §F-R11).
- [ ] `Finance.Tests.Unit` ≥ 60 tests green.
- [ ] `Finance.IntegrationTests` ≥ 15 tests green.
- [ ] Permission catalog parity test passes (10-cross-cutting.md §3).
- [ ] PCI audit tests pass (10-cross-cutting.md §7).

## 2. Endpoint Smoke Test

| # | Method | Path | Expected | Notes |
|---|---|---|---|---|
| 1 | POST | `/payments/initiate` | 201 + `paymentId, gatewayPaymentId, redirectUrl` | T1 happy |
| 2 | POST | `/payments/initiate` | 422 `Payment.BookingNotEligible` | T1, booking not in AwaitingPayment |
| 3 | POST | `/payments/initiate` | 200 + existing details (idempotent) | T1, duplicate call |
| 4 | POST | `/payments/webhook` (signed) | 200 | T2 happy |
| 5 | POST | `/payments/webhook` (bad signature) | 403 `Payment.WebhookSignatureMismatch` | T2 |
| 6 | POST | `/payments/webhook` (duplicate eventId) | 200 noop | T2 idempotency |
| 7 | POST | `/payments/{id}/refund` | 201 + refund | T2 happy |
| 8 | POST | `/payments/{id}/refund` | 422 `Refund.AmountExceedsRefundable` | T2 |
| 9 | POST | `/payments/{id}/refund` | 409 `Refund.PaymentNotCompleted` | T2 |
| 10 | GET | `/payments/{id}` | 200 owner/provider/admin | T2 |
| 11 | GET | `/payments/{id}` | 403 stranger | T2 |
| 12 | GET | `/payments/my-payments` | 200 cursor | T2 |
| 13 | GET | `/payments/admin/all` | 200 + filters | T2 |
| 14 | GET | `/invoices/my-invoices` | 200 | T3 |
| 15 | GET | `/invoices/provider/my-invoices` | 200 (provider role) | T3 |
| 16 | GET | `/invoices/{id}` | 200 | T3 |
| 17 | GET | `/invoices/{id}/download` | 200 application/pdf | T3 |
| 18 | GET | `/payouts/admin/pending` | 200 list | T4 |
| 19 | GET | `/payouts/provider` | 200 self-filter | T4 |
| 20 | GET | `/payouts/{id}` | 200 | T4 |
| 21 | POST | `/payouts/admin/trigger` | 202 + `{created, skipped, onHold}` summary | T4 |
| 22 | POST | `/payouts/{id}/approve` | 200, status Completed (small) OR Pending until gateway webhook (large) | T4 |
| 23 | POST | `/payouts/{id}/approve` | 409 `Payout.NotPending` (already approved) | T4 |
| 24 | GET | `/commissions` | 200 list | T6 |
| 25 | POST | `/commissions` | 201 + commissionRuleId | T6 |
| 26 | POST | `/commissions` | 409 `CommissionRule.OverlapTier` | T6 |
| 27 | PUT | `/commissions/{id}` | 200 | T6 |
| 28 | DELETE | `/commissions/{id}` | 204 + outbox row | T6 |

## 3. Outbox / Inbox Round-Trip

| Action | Outbox name | Downstream effect |
|---|---|---|
| Payment succeeds (T2) | `finance.payment.completed.v1` | Booking marks booking Confirmed; Invoice generates (T3 handler); Messaging sends receipt |
| Payment fails (T2) | `finance.payment.failed.v1` | Booking releases SlotLock + restores capacity |
| Refund completes (T2) | `finance.refund.completed.v1` | Booking updates refundedAmount; Messaging sends refund confirmation |
| Refund finally fails after 3 retries (T5) | `finance.refund.failed.v1` | Messaging admin alert (page on-call) |
| Invoice generated (T3) | `finance.invoice.generated.v1` | Messaging "invoice ready" email |
| Payout batch created (T5) | `finance.payout.scheduled.v1` | Messaging provider summary email |
| Payout completed (T4 approve) | `finance.payout.completed.v1` | Messaging notifies provider with gateway reference |
| Commission rule upserted (T6) | `finance.commission-rule.upserted.v1` | Booking refreshes BookingCommissionSnapshot |
| Commission rule deleted (T6) | `finance.commission-rule.deleted.v1` | Booking removes snapshot |

## 4. Background Services Live Test (24h soak)

- [ ] `RefundRetryService` ticks ≈ 96 times in 24h; processes any failed refund within 15 min of failure.
- [ ] `PayoutBatchingService` schedules next Sunday correctly; manual override `TargetTimeUtc=now+2min` triggers a batch.
- [ ] OTEL counters all zero failures.
- [ ] No uncaught exceptions in Serilog file.

## 5. Performance Sanity

| Endpoint | p95 | Hard ceiling |
|---|---|---|
| POST `/payments/initiate` | < 600 ms | < 1.5 s (gateway call dominates) |
| POST `/payments/webhook` | < 200 ms | < 500 ms |
| POST `/payments/{id}/refund` | < 800 ms | < 2 s |
| GET `/payments/{id}` (cached) | < 80 ms | < 200 ms |
| GET `/invoices/{id}/download` first render | < 2 s | < 5 s |
| GET `/invoices/{id}/download` cached PDF | < 300 ms | < 600 ms |
| POST `/payouts/admin/trigger` 100 providers | < 5 s | < 15 s |

## 6. Documentation Hygiene
- [ ] XML docs on every new Command/Query record.
- [ ] All 22 Finance permissions listed in `Agents/permissions-inventory.md`.
- [ ] Sprint folder moves to `Agents/decisions/closed/Finance/` (preserve all 9 files).
- [ ] Master `Phase1-Phase2-Completion-INDEX.md` Finance row → 🟢 + closed/ link.
- [ ] `AGENTS.md` (repo-root) updated.
- [ ] `agent-context.md §11.1` Finance row → ✅ Complete (with caveat: "Phase 1 only — Discount/Dispute/Loyalty/Referral/Subscription deferred to Phase 3").
- [ ] `Agents/error-log.md` new entries for any new gotchas hit.
- [ ] `Agents/decisions/` — ADR-007 "Commission CRUD lives in Finance; Booking holds snapshot via inbox" (already proposed in Booking sprint, FINALIZE here).

## 7. Sprint Retro & Demo (Fri 2026-10-16 11:00 AST)

Demo by Mohammad:
1. Live POST /payments/initiate → gateway redirect → simulated success webhook → booking Confirmed.
2. Refund flow demo with refund failure + retry recovery.
3. Weekly payout batch live demo (advance clock) → admin approves → gateway completes.
4. Commission rule edit demo + verify Booking inbox snapshot updates within 30s.
5. Invoice PDF download (visual quality check).

Retro doc at `Agents/decisions/closed/Finance/_retro.md`.

## 8. Sign-Off

| Role | Name | Date | Signature |
|---|---|---|---|
| T1 owner | Mahmoud | _____ | _____ |
| T2 owner | Mahmoud | _____ | _____ |
| T3 owner | Fadwa | _____ | _____ |
| T4 owner | Mohammad | _____ | _____ |
| T5 owner | Mohammad | _____ | _____ |
| T6 owner | _____ | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |

---

