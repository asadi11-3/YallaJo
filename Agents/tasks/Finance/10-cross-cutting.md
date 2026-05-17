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
