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
