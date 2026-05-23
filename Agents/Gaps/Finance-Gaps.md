# Finance Module — Audit Report

> Audited: 2025-01-XX | Sources of Truth: `agent-context.md`, `YallaJo.md`, `Endpoints.pdf`, `YallaJo Business Rules & Edge Cases.pdf`

---

## 1. Module Overview

**Purpose**: Handles payments, refunds, escrow, payouts, invoices, commissions, and (planned) subscriptions, discounts, loyalty, referrals, and disputes for the YallaJo platform.

| Layer | Project | Files |
|-------|---------|-------|
| Domain | `Finance.Domain` | 20 entities, 15 enums, 18 domain events, 10 repository interfaces |
| Application | `Finance.Application` | 55 files: 8 commands, 11 queries, 5 event handlers, 6 interfaces, 3 validators |
| Contracts | `Finance.Contracts` | 7 features, 20 permissions, 13 integration events, 4 cross-module contracts |
| Infrastructure | `Finance.Infrastructure` | 62 files: 21 EF configs, 10 repos, 2 background services, 1 gateway, 10 outbox converters |
| Presentation | `Finance.Presentation` | 4 endpoint groups, 19 endpoints |
| Tests | `Finance.Tests.Unit` + `Finance.IntegrationTests` | 3 test files |
| **Total** | | **~190 files** |

### Entities (20)

| Entity | Lines | Status |
|--------|-------|--------|
| Payment | 456 | Full aggregate: state machine, escrow, refund sibling pattern |
| Payout | 242 | Full aggregate: batch, hold, approve, complete lifecycle |
| PayoutItem | ~35 | Child entity with guard throws |
| Invoice | 177 | Full aggregate: generation, PDF, cancel-on-refund |
| InvoiceItem | ~25 | Owned child entity |
| CommissionRule | 119 | Aggregate: tier+revenue range, overlap prevention |
| PaymentExpectation | ~48 | Booking payment snapshot/projection |
| ProviderBankAccount | ~20 | Schema-only, no domain logic |
| Discount | 37 | **BARE**: properties only, NO state machine/factory/events |
| DiscountUsage | ~15 | Schema-only |
| LoyaltyPoints | 17 | **BARE**: properties only |
| LoyaltyTransaction | ~15 | Schema-only |
| Referral | 17 | **BARE**: properties only |
| Subscription | 19 | **BARE**: properties only |
| SubscriptionPlan | ~20 | Schema-only |
| SubscriptionFeature | ~15 | Catalog row |
| PlanFeature | ~10 | Join table |
| Dispute | 26 | **BARE**: properties only, NO state machine |
| DisputeMessage | ~15 | Schema-only |
| DisputeEvidence | ~15 | Schema-only |

### Enums (15)

BillingCycle, DiscountTargetScope, DiscountType, DiscountVisibility, DisputeResolution, DisputeStatus, EvidenceType, InvoiceStatus, PaymentExpectationStatus, PaymentMethod, PaymentStatus, PaymentType, PayoutStatus, SubscriptionStatus, TransactionType

### Repositories (10)

IPaymentRepository, IPayoutRepository, IPayoutItemRepository, IInvoiceRepository, ICommissionRuleRepository, IPaymentExpectationRepository, IProviderBankAccountRepository, ISubscriptionRepository, IDisputeRepository, IFinanceOutboxWriter

### Integration Events — Outbound (13)

PaymentCompleted, PaymentFailed, RefundInitiated, RefundCompleted, RefundFailed, PayoutScheduled, PayoutCompleted, InvoiceGenerated, CommissionRuleUpserted, CommissionRuleDeleted, SubscriptionActivated, SubscriptionCancelled, DisputeOpened

### Permission Catalog (20 permissions across 7 features)

| Feature | Permissions | Group |
|---------|-------------|-------|
| Payment | Create, Read | FinanceOperations |
| Refund | Create, Read | FinanceOperations |
| Invoice | Read, Download | FinanceOperations |
| Payout | Read, Trigger, Approve | FinanceOperations |
| CommissionRule | Create, Read, Update, Delete | FinanceOperations |
| ProviderBankAccount | Create, Update, Read, Verify | FinanceOperations |
| AdminFinanceDashboard | Read, Export, Refresh | FinanceOperations |

---

## 2. Architecture Compliance

### Dependency Graph

```
Presentation → Application → Domain
                    ↓
              Contracts (cross-module)
Infrastructure → Application + Domain
```

**CQRS**: All 8 commands and 11 queries go through MediatR. No endpoint-level CQRS bypass. ✅

---

## 3. Endpoint Security Audit

### PaymentEndpoints.cs (6 endpoints)

| Method | Route | Auth |
|--------|-------|------|
| POST | `/api/v1/payments/initiate` | MustHavePermission(Payment, Create) + RequireAuthorization ✅ |
| POST | `/api/v1/payments/webhook` | AllowAnonymous (HMAC-verified) ✅ |
| POST | `/api/v1/payments/{id}/refund` | MustHavePermission(Refund, Create) + RequireAuthorization ✅ |
| GET | `/api/v1/payments/{id}` | MustHavePermission(Payment, Read) + RequireAuthorization ✅ |
| GET | `/api/v1/payments/my-payments` | MustHavePermission(Payment, Read) + RequireAuthorization ✅ |
| GET | `/api/v1/payments/admin/all` | MustHavePermission(AdminFinanceDashboard, Read) + RequireAuthorization ✅ |

### PayoutEndpoints.cs (5 endpoints)

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/payouts/admin/pending` | MustHavePermission(Payout, Read) + RequireAuthorization ✅ |
| GET | `/api/v1/payouts/provider` | MustHavePermission(Payout, Read) + RequireAuthorization ✅ |
| GET | `/api/v1/payouts/{id}` | MustHavePermission(Payout, Read) + RequireAuthorization ✅ |
| POST | `/api/v1/payouts/admin/trigger` | MustHavePermission(Payout, Trigger) + RequireAuthorization ✅ |
| POST | `/api/v1/payouts/{id}/approve` | MustHavePermission(Payout, Approve) + RequireAuthorization ✅ |

### CommissionRuleEndpoints.cs (4 endpoints)

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/commission-rules` | MustHavePermission(CommissionRule, Read) + RequireAuthorization ✅ |
| POST | `/api/v1/commission-rules` | MustHavePermission(CommissionRule, Create) + RequireAuthorization ✅ |
| PUT | `/api/v1/commission-rules/{id}` | MustHavePermission(CommissionRule, Update) + RequireAuthorization ✅ |
| DELETE | `/api/v1/commission-rules/{id}` | MustHavePermission(CommissionRule, Delete) + RequireAuthorization ✅ |

### InvoiceEndpoints.cs (4 endpoints)

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/invoices/my-invoices` | MustHavePermission(Invoice, Read) + RequireAuthorization ✅ |
| GET | `/api/v1/invoices/provider/my-invoices` | MustHavePermission(Invoice, Read) + RequireAuthorization ✅ |
| GET | `/api/v1/invoices/{id}` | MustHavePermission(Invoice, Read) + RequireAuthorization ✅ |
| GET | `/api/v1/invoices/{id}/download` | MustHavePermission(Invoice, Download) + RequireAuthorization ✅ |

**Result: 19/19 endpoints correctly decorated** ✅

---

## 4. Rule Compliance Matrix

| # | Rule | Status | Evidence |
|---|------|--------|----------|
| 1 | MustHavePermission on every endpoint | ✅ PASS | 19/19 endpoints (webhook is AllowAnonymous + HMAC) |
| 2 | ICurrentUser for ownership only | ⚠️ REVIEW | Zero in Application; 11 endpoints have redundant auth gate |
| 3 | Result pattern (no throw in Application) | ✅ PASS | Zero `throw new` in 55 Application files |
| 4 | Per-module IPermissionCatalog | ✅ PASS | FinancePermissionCatalog with 20 permissions |
| 5 | No SaveChanges in domain event handlers | ✅ PASS | 1 domain handler (OnPaymentCompletedGenerateInvoice) — zero SaveChanges |
| 6 | DateTime.UtcNow (no .Now/.Today) | ✅ PASS | Zero violations in Application + Domain |
| 7 | Guid.CreateVersion7 (no .NewGuid) | ⚠️ MINOR | 1 in PaymentEndpoints.cs:135 (webhook fallback), 1 in FakePaymentGateway |
| 8 | HybridCache invalidation | ❌ MISSING | Zero HybridCache usage in entire module |
| 9 | FluentValidation on commands | ⚠️ PARTIAL | 3/8 commands have validators; 5 missing |
| 10 | Outbox pattern | ✅ PASS | 10 domain→outbox converters, zero SaveChanges in converters |
| 11 | Domain guards (throws) | ✅ PASS | 38 throws across 6 domain entities — DDD acceptable |
| 12 | SaveChanges in query handler | ⚠️ VIOLATION | DownloadInvoiceQuery.cs:63 persists PDF path in a query |

---

## 5. Gap #1 — Redundant ICurrentUser Auth Gates at Endpoint Level (MEDIUM)

### Severity: MEDIUM
### Impact: Code complexity; redundant with `.RequireAuthorization()` middleware
### Rule Violated: §0 Rule 2 — "ICurrentUser: only for ownership checks"

### Affected Endpoints (~11 of 19)

- `PaymentEndpoints.cs` — 5 endpoints check `IsAuthenticated` / `UserId is null`
- `PayoutEndpoints.cs` — 3 endpoints check `IsAuthenticated` / `UserId is null`
- `InvoiceEndpoints.cs` — 4 endpoints check `IsAuthenticated` / `UserId is null`
- `CommissionRuleEndpoints.cs` — 0 violations ✅

### Violation Pattern

```csharp
// Endpoint already has .RequireAuthorization() + .MustHavePermission(...)
// But ALSO does:
var currentUser = http.RequestServices.GetRequiredService<ICurrentUser>();
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Results.Unauthorized();
```

### Why Wrong

The `.RequireAuthorization()` middleware already rejects unauthenticated requests. The manual check is dead code that adds confusion.

### Required Fix

Remove `IsAuthenticated` / `UserId is null` guards from all endpoints that already have `.RequireAuthorization()`. Keep only ownership scoping logic (e.g., `CallerUserId` passed to query).

---

## 6. Gap #2 — 6 Bare Entities with No Domain Logic (HIGH)

### Severity: HIGH
### Impact: Entire spec features are schema-only — no business rules, state machines, factories, or domain events
### Rule Violated: Spec compliance (§15-19, §20-24 of Business Rules PDF)

### Affected Entities

| Entity | Lines | Spec Requirement | Implementation |
|--------|-------|-----------------|----------------|
| Discount | 37 | 5 types, 3 visibilities, state machine, 20 max active, stacking rules, 5 JOD minimum | Properties only |
| LoyaltyPoints | 17 | 10 pts/JOD, FIFO expiry 12mo, 100pts=1JOD, max 50% redemption, birthday bonus | Properties only |
| Referral | 17 | YJ-XXXXXX codes, reward tracking, 1.5x subscriber multiplier | Properties only |
| Subscription | 19 | 4 tiers, 30-day trial, feature gating, renewal grace, billing cycle | Properties only |
| Dispute | 26 | 7-day filing window, 48h response, 4 resolutions, payout freeze | Properties only |
| SubscriptionPlan | ~20 | Plan catalog with pricing and feature sets | Properties only |

### Why This Is Critical

The spec defines complete business workflows for each of these features. The current implementation has DB tables and EF configurations but **zero domain logic, zero handlers, zero endpoints, and zero validators**. They are effectively placeholder schemas.

---

## 7. Gap #3 — Missing Endpoints for Existing Permissions (HIGH)

### Severity: HIGH
### Impact: 7 permissions in catalog with zero endpoints; entire feature areas unreachable
### Rule Violated: Architecture consistency — permissions should map to endpoints

### Dead Permissions

| Feature | Permission | Endpoints |
|---------|-----------|-----------|
| ProviderBankAccount | Create | NONE |
| ProviderBankAccount | Update | NONE |
| ProviderBankAccount | Read | NONE |
| ProviderBankAccount | Verify | NONE |
| AdminFinanceDashboard | Read | Used by GET `/admin/all` only (Payment feature, not Dashboard) |
| AdminFinanceDashboard | Export | NONE |
| AdminFinanceDashboard | Refresh | NONE |

### Impact

- Providers cannot add/update/verify bank accounts (required for payout disbursement)
- No admin finance dashboard endpoints (stats, export, refresh)
- 5 truly dead permissions, 2 misused (Dashboard Read used for payment listing)

---

## 8. Gap #4 — Commission Model Divergence (MEDIUM)

### Severity: MEDIUM
### Impact: Commission calculation may not match business expectations
### Rule Violated: Spec §6 Payment & Refund

### Spec Requirement

Tiered commission by subscription tier:
- Free: 15%
- Basic: 10%
- Premium: 7%
- Enterprise: Custom

### Implementation

`CommissionRule.cs` uses `Tier + MinMonthlyRevenue + MaxMonthlyRevenue + Currency + Percentage` model. Commission is determined by revenue range, not subscription tier.

`CommissionLookupService.cs` reads from `CommissionRule` table. `SubscriptionStatusProvider.cs` exists but only provides `HasActiveSubscriptionAsync` — it is NOT integrated into commission calculation.

### Required Decision

Either:
- A) Align to spec: commission = f(subscription_tier)
- B) Keep current: commission = f(revenue_range) — update spec
- C) Hybrid: base rate from tier, adjusted by volume

---

## 9. Gap #5 — Missing Validators (MEDIUM)

### Severity: MEDIUM
### Impact: 5 commands accept unvalidated input
### Rule Violated: §4 FluentValidation (every command must have a validator)

### Missing Validators

| Command | File | Why Needed |
|---------|------|------------|
| UpdateCommissionRuleCommand | `Commands/UpdateCommissionRule/` | Percentage range, revenue range, tier validation |
| DeleteCommissionRuleCommand | `Commands/DeleteCommissionRule/` | Id validation |
| ProcessWebhookCommand | `Commands/ProcessWebhook/` | Payload structure, signature presence |
| TriggerPayoutCommand | `Commands/TriggerPayout/` | Date range, optional provider filter |
| ApprovePayoutCommand | `Commands/ApprovePayout/` | PayoutId, approver context |

### Existing Validators (3)

- `CreateCommissionRuleCommandValidator.cs` ✅
- `InitiatePaymentCommandValidator.cs` ✅
- `RefundPaymentCommandValidator.cs` ✅

---

## 10. Gap #6 — SaveChangesAsync in Query Handler (LOW)

### Severity: LOW
### Impact: Side effect in read path violates CQRS separation
### Rule Violated: CQRS — queries should not mutate state

### Location

`Finance.Application/Queries/DownloadInvoice/DownloadInvoiceQuery.cs:63`

```csharp
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

### Context

The handler lazily renders the invoice PDF on first download, then persists the PDF file path back to the Invoice entity. This is a legitimate optimization but violates strict CQRS.

### Suggested Fix

Extract PDF rendering into a command that runs on `InvoiceGenerated` domain event (eager render), or accept this as a documented exception.

---

## 11. Gap #7 — Zero HybridCache (LOW)

### Severity: LOW
### Impact: No cache invalidation patterns; repeated DB reads for commission rules, invoices, payouts
### Rule Violated: §5 HybridCache pattern

### Context

Every other audited module uses HybridCache (ContentCore: 20+, ContentTours: 29, ContentBlogs: 40, ContentSeo: 10, Analytics: 5). Finance has **zero** HybridCache usage.

### Recommended Cache Targets

- Commission rules (rarely change, frequently queried)
- Invoice lookups by ID
- Provider payout summaries

---

## 12. What Passed — Full Checklist

### Endpoint Auth (19/19) ✅

All 19 endpoints have explicit auth decorations. The webhook endpoint uses AllowAnonymous with HMAC signature verification. No missing permissions.

### Result Pattern ✅

Zero `throw new` in Application layer. All 8 command handlers consistently use `Result.Success` / `Result.Failure`. Domain guard exceptions are caught and translated to `Result.Failure` in handlers.

### SaveChanges in Event Handlers ✅

- 1 domain event handler (`OnPaymentCompletedGenerateInvoiceHandler`): zero SaveChanges ✅
- 4 integration event handlers in Application: SaveChanges in own scope ✅
- 10 outbox converters: zero SaveChanges ✅
- 1 infrastructure integration handler (`ProviderSuspendedHoldPayoutsHandler`): SaveChanges in own scope ✅

### DateTime/GUID Compliance ✅

- Zero `DateTime.Now` or `DateTime.Today` in Application + Domain
- Zero `Guid.NewGuid()` in Application + Domain
- 2 `Guid.NewGuid()` in Presentation + Infrastructure (minor convention violation)

### Domain Logic — Payment/Payout/Invoice ✅

- **Payment**: Full state machine (Pending→Processing→Completed/Failed, Completed→PartiallyRefunded/Refunded). Money VO for amounts. Idempotent completion/failure. Escrow eligibility with configurable hold period.
- **Payout**: Full batch lifecycle (Pending→Hold/ReadyForPayout→Completed/Failed). Admin approval. Running totals with gross-commission=net.
- **Invoice**: Per-booking generation on PaymentCompleted. INV-{YYYYMM}-{seq6} numbering. Banker's rounding. Cancel-on-refund.
- **CommissionRule**: Overlap prevention. Deactivation (soft delete). Domain events.

### Escrow (7-day hold) ✅

`Payment.MarkEscrowEligible(completedAtUtc, holdPeriod)` — default 7 days. Matches spec's "7-day escrow hold".

### Min Payout Threshold (10 JOD) ✅

`TriggerPayoutOptions.MinPayoutThreshold = 10m` — enforced in `TriggerPayoutCommandHandler.cs:103`. Matches spec.

### Background Services ✅

- **PayoutBatchingService**: Weekly Sunday midnight UTC. Matches spec's "weekly Sunday batch".
- **RefundRetryService**: Every 15 minutes, MaxRetries=3, BatchSize=200. Matches spec's retry service.

### Outbox/Inbox Pattern ✅

10 domain→outbox converters in `FinanceIntegrationConverters.cs`. Inbox store for webhook idempotency. Zero SaveChanges in converters.

### Cross-Module Integration ✅

- Inbound: 4 booking integration events (Created, Completed, Cancelled, PaymentExpired) + 1 ProviderSuspended
- Outbound: 13 integration events via outbox
- Contracts: IPaymentGateway, ICommissionLookupService, ISubscriptionStatusProvider

### Invoice Generation ✅

QuestPDF renderer. LocalFileInvoiceStorage. Race-safe SqlInvoiceNumberGenerator with retry. PaymentRedactor for sensitive data.

---

## 13. Scorecard

| Area | Score | Notes |
|------|-------|-------|
| Endpoint Auth | 10/10 | 19/19 correct, webhook HMAC |
| ICurrentUser | 7/10 | Zero in Application, 11 redundant gates in Presentation |
| Result Pattern | 10/10 | Zero Application throws |
| Domain Logic (Core) | 9/10 | Payment/Payout/Invoice/Commission fully modeled |
| Domain Logic (Planned) | 2/10 | 6 entities are schema-only shells |
| SaveChanges Rules | 9/10 | 1 query-side SaveChanges |
| Validators | 5/10 | 3/8 commands validated |
| Caching | 2/10 | Zero HybridCache |
| Convention Compliance | 9/10 | 2 minor Guid.NewGuid() |
| Test Coverage | 2/10 | 3 test files |
| **Overall** | **5.5/10** | Core payment pipeline solid; 6 feature areas unimplemented |

---

## 14. Fix Priority & Recommendations

### P1 — Critical (blocks spec compliance)

1. **Implement Discount domain logic + endpoints** — Spec §24: 5 types, 3 visibilities, stacking rules, state machine, 20 active max, 5 JOD minimum. ~20-30h
2. **Implement Subscription domain logic + endpoints** — Spec §18: 4 tiers, trial, feature gating, renewal grace, billing cycle. ~15-20h
3. **Implement Loyalty Points domain logic + endpoints** — Spec §17: FIFO expiry, redemption, birthday bonus, subscriber multiplier. ~10-15h
4. **Implement Dispute domain logic + endpoints** — Spec §19: filing window, response deadline, 4 resolutions, payout freeze. ~10-15h
5. **Implement Referral domain logic + endpoints** — Spec §17: code generation, reward tracking, multiplier. ~8-10h
6. **Add ProviderBankAccount endpoints** — Required for payout disbursement. ~4h

### P2 — High (architecture violations)

7. **Add 5 missing command validators** — ~2h
8. **Resolve commission model divergence** — Decide spec vs implementation. ~2-4h
9. **Remove redundant ICurrentUser auth gates** — ~2h
10. **Add AdminFinanceDashboard endpoints or remove dead permissions** — ~3h

### P3 — Medium (convention violations)

11. **Add HybridCache to commission rules + invoices** — ~2h
12. **Fix DownloadInvoice SaveChanges-in-query** — ~1h
13. **Replace 2 Guid.NewGuid() with Guid.CreateVersion7** — ~10min

### P4 — Low (quality)

14. **Add unit tests** — Current: 3 files. Target: 30+. ~15-20h

---

## 15. Appendix — Files Audited

### Domain (64 files)
- 20 entity files (all read in full)
- 15 enum files
- 18 domain event files
- 10 repository interface files
- 1 value object (Money from SharedKernel)

### Application (55 files)
- 8 command handler sets (command + handler + optional validator)
- 11 query files
- 5 event handlers
- 6 interfaces
- 4 DTO files
- 1 DependencyInjection.cs

### Infrastructure (62 files)
- 21 EF configurations
- 10 repositories
- 10 outbox converters
- 1 integration event handler
- 2 background services
- 1 payment gateway
- 2 services (CommissionLookup, SubscriptionStatus)
- 1 PDF renderer + 1 storage + 1 security
- 1 invoice number generator
- 6 migrations + 1 seeder

### Presentation (7 files)
- 4 endpoint groups + 2 request DTOs + 1 registration

### Tests (3 files)
- FinancePermissionCatalogTests.cs
- FinanceDbContextScaffoldTests.cs
- PaymentRedactorTests.cs
