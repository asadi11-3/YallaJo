# Finance module — Payments / Payouts / Invoices (ERD 1 of 2)

**DbContext:** `FinanceDbContext` · **Schema:** `finance`

This diagram covers the payment, payout, and invoicing aggregates. Disputes, discounts and
provider banking are in [`finance-disputes-discounts.md`](finance-disputes-discounts.md).

> Finance makes heavy use of the **`Money` owned type** (`OwnsOne`) — each money field maps
> to an `Amount` + `Currency` column pair. `Invoice` owns a collection of `InvoiceItem`
> (`OwnsMany`), each of which itself owns two `Money` values.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 | Notes |
|--------|------|----------------|----|----|-------|
| `Payment` | AuditableEntity | ✅ | ✅ | ✅ | owns 3× Money |
| `PaymentExpectation` | BaseEntity | ❌ | ❌ | ❌ | read-side projection 📸 |
| `Payout` | AuditableEntity | ✅ | ✅ | ✅ | owns 4× Money |
| `PayoutItem` | BaseEntity | ❌ | parent | ❌ | owns 3× Money |
| `Invoice` | AuditableEntity | ✅ | ✅ | ✅ | owns 4× Money |
| `InvoiceItem` ◆ | BaseEntity (owned) | ❌ | (owner) | ❌ | OwnsMany |
| `InvoiceNumberCounter` | plain | ❌ | ❌ | ✅ | PK = `YearMonth` (string) |
| `OutboxMessage` / `InboxMessage` | infra | — | — | — | |

## Diagram

```mermaid
erDiagram
    Payment {
        guid Id PK
        guid UserId LREF
        guid ProviderId LREF
        guid BookingId LREF "nullable"
        guid OriginalPaymentId "self LREF, no FK"
        decimal Amount "◆ Money"
        decimal RefundedTotal "◆ Money"
        string Status
    }
    PaymentExpectation {
        guid Id PK
        guid BookingId LREF
        guid UserId LREF
        guid ProviderId LREF
        guid TourId LREF
        guid PaymentId LREF "nullable, no FK"
        decimal ExpectedAmount "◆ Money"
    }
    Payout {
        guid Id PK
        guid ProviderId LREF
        guid BankAccountId LREF "nullable, no FK"
        decimal GrossAmount "◆ Money"
        decimal CommissionAmount "◆ Money"
        decimal NetAmount "◆ Money"
    }
    PayoutItem {
        guid Id PK
        guid PayoutId FK
        guid BookingId LREF
        guid CommissionRuleSnapshotId LREF "no FK"
        decimal NetAmount "◆ Money"
    }
    Invoice {
        guid Id PK
        string InvoiceNumber
        guid PaymentId LREF "unique index, no FK"
        guid BookingId LREF
        guid UserId LREF
        guid ProviderId LREF
        decimal AmountTotal "◆ Money"
    }
    InvoiceItem {
        guid Id PK
        guid InvoiceId FK "owned"
        decimal UnitPrice "◆ Money"
        decimal Subtotal "◆ Money"
    }
    InvoiceNumberCounter {
        string YearMonth PK
        int Counter
    }

    Payout ||--o{ PayoutItem : "PayoutId (Restrict)"
    Invoice ||--o{ InvoiceItem : "Items (OwnsMany)"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| Payout → PayoutItem | `PayoutId` | Restrict |
| Invoice → InvoiceItem (owned collection) | `InvoiceId` | (owned) |

> `Payment` → `Dispute` is shown in the disputes diagram.

## Owned types (`Money` unless noted)

- `Payment`: `Amount`, `RefundedAmount`, `RefundedTotal`
- `Payout`: `GrossAmount`, `CommissionAmount`, `NetAmount`, `TotalAmount` (legacy mirror)
- `PayoutItem`: `GrossAmount`, `CommissionAmount`, `NetAmount`
- `Invoice`: `AmountSubtotal`, `AmountTax`, `AmountDiscount`, `AmountTotal`
- `Invoice.Items` → **`OwnsMany InvoiceItem`** (table `InvoiceItems`); each `InvoiceItem`
  owns `UnitPrice` + `Subtotal` (Money).
- `PaymentExpectation.ExpectedAmount`

## Cross-module logical references (no DB FK)

- `UserId`, `ProviderId`, `BookingId`, `TourId` (see central catalog).

## Edge cases — same-module, no FK

- `Invoice.PaymentId` (unique index → de-facto 1:1, **no FK**), `Payment.OriginalPaymentId?`
  (self-ref refund), `Payout.BankAccountId?` (→ ProviderBankAccount),
  `PayoutItem.CommissionRuleSnapshotId?` (→ CommissionRule).

## Notes / unclear

- `PaymentExpectation` is a Finance-local read-model (📸) of a Booking event.
- `InvoiceNumberCounter` is an infrastructure helper (string PK `YearMonth`, RowVersion for
  optimistic sequence increments) — not a domain aggregate.
- Legacy mirror columns exist (`Payout.TotalAmount`/`TransactionId`, `Payment.TransactionId`).
