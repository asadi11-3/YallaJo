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
