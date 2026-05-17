# Finance — Pre-Work Kickoff Briefing

> Sprint window: 2026-08-17 → 10-15 (Wave 5 second half). Owner: Mohammad.

## What's already wired

- `IFinanceUnitOfWork` interface + `FinanceUnitOfWork` delegate.
- 11 aggregate roots marked `IAggregateRoot`: Payment, Payout, InvoiceItem, Subscription, SubscriptionPlan, Discount, CommissionRule, Dispute, Referral, LoyaltyPoints, ProviderBankAccount. Eight child entities (DiscountUsage, DisputeEvidence, DisputeMessage, InvoiceLineItem, LoyaltyTransaction, PayoutItem, PlanFeature, SubscriptionFeature) remain bare `AuditableEntity` by design.
- 14 domain events in `Finance.Domain/Events/`, 10 integration events in `Finance.Contracts/IntegrationEvents/` registered as `finance.{aggregate}.{action}.v1`.
- 6 repositories: IPaymentRepository (+GetByGatewayTransactionIdAsync), IPayoutRepository (+GetPendingAsync), IInvoiceItemRepository (+GetOverdueAsync), IDisputeRepository (+GetOpenDisputesAsync), ISubscriptionRepository (+GetExpiringAsync), IFinanceOutboxWriter.
- `IPaymentGateway` + records (PaymentChargeRequest/Result, PaymentRefundRequest/Result) + `FakePaymentGateway` stub (deterministic tx id from `IdempotencyKey`).
- `FinanceFeatures` (12) + `FinancePermissionCatalog` (22 perms) registered.
- **PCI baseline (PW-9):** `Finance.Infrastructure/Security/PaymentRedactor.cs` — `static partial class` with `GeneratedRegex` for PAN (mask middle), CVV in JSON/query-string, IBAN.
- Test projects in YallaJo.sln. `PaymentRedactorTests` lives in `IntegrationTests` (because PaymentRedactor is in Infrastructure).

## Day-0 sprint tasks

1. EF migration `FinanceAddAggregateRootAndAuditMembers` + UNIQUE index on `Payment.GatewayTransactionId` (per PW-9).
2. Replace `FakePaymentGateway` with real provider integration (Stripe-style) behind `IPaymentGateway`. Webhook endpoint must verify signature via `VerifyWebhookSignatureAsync`.
3. Enforce HTTPS-only for the webhook endpoint (binding + middleware).
4. Audit-log every `Payment` state change (`AuditableEntity` already provides `RowVersion`; add explicit audit emit on `PaymentSucceeded/Failed/Refunded`).
5. Secrets via environment variables only — confirm `IConfiguration["Payment:Stripe:WebhookSecret"]` reads from env, never appsettings.json.

## Watchpoints

- `Subscription.ActivePeriod` is a `DateRange?` value object — null = inactive. Repository `GetExpiringAsync(DateTime threshold)` already filters on `.End < threshold`.
- `Money` value object on `Payment.Amount`, `Payout.TotalAmount`, `InvoiceItem.TotalAmount` — never log Money raw, always `PaymentRedactor.Redact()` the surrounding payload.
- Outbox writer uses `OutboxMessage.Create(integrationEvent)` (factory; private setters).
