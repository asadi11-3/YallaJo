# Finance module — Disputes / Discounts / Provider banking (ERD 2 of 2)

**DbContext:** `FinanceDbContext` · **Schema:** `finance`

Covers disputes (with messages & evidence), discounts (with usage), commission rules, and
provider banking/payment methods. Payments/payouts/invoices are in
[`finance-payments.md`](finance-payments.md).

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `Dispute` | AuditableEntity | ✅ | ✅ | ✅ |
| `DisputeMessage` | BaseEntity | ❌ | parent | ❌ |
| `DisputeEvidence` | BaseEntity | ❌ | parent | ❌ |
| `Discount` | AuditableEntity | ✅ | ✅ | ✅ |
| `DiscountUsage` | BaseEntity | ❌ | parent | ❌ |
| `CommissionRule` | AuditableEntity | ✅ | ✅ | ✅ |
| `ProviderBankAccount` | AuditableEntity | ✅ | ✅ | ✅ |
| `ProviderPaymentMethod` | AuditableEntity | ✅ | ✅ | ✅ |

## Diagram

```mermaid
erDiagram
    Payment {
        guid Id PK
        string Status
    }
    Dispute {
        guid Id PK
        guid PaymentId FK
        guid UserId LREF
        guid ResolvedByUserId LREF "nullable"
        string Status
    }
    DisputeMessage {
        guid Id PK
        guid DisputeId FK
        guid SenderUserId LREF
        string Body
    }
    DisputeEvidence {
        guid Id PK
        guid DisputeId FK
        guid UploadedByUserId LREF
        string FileUrl
    }
    Discount {
        guid Id PK
        string Code
        guid ProviderId LREF "nullable"
        guid TourId LREF "nullable"
        guid CategoryId LREF "nullable"
        decimal DiscountValue "◆ Money"
        datetime ValidFrom "◆ DateRange"
    }
    DiscountUsage {
        guid Id PK
        guid DiscountId FK
        guid UserId LREF
        guid BookingId LREF "nullable"
        decimal Amount "◆ Money"
    }
    CommissionRule {
        guid Id PK
        string Tier
        decimal Rate
    }
    ProviderBankAccount {
        guid Id PK
        guid UserId LREF
        string Iban
    }
    ProviderPaymentMethod {
        guid Id PK
        guid UserId LREF
        guid VerifiedByAdminId LREF "nullable"
    }

    Payment ||--o{ Dispute : "PaymentId (Restrict)"
    Dispute ||--o{ DisputeMessage : "Cascade"
    Dispute ||--o{ DisputeEvidence : "Cascade"
    Discount ||--o{ DiscountUsage : "DiscountId (Restrict)"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| Payment → Dispute | `PaymentId` | Restrict |
| Dispute → DisputeMessage | `DisputeId` | Cascade |
| Dispute → DisputeEvidence | `DisputeId` | Cascade |
| Discount → DiscountUsage | `DiscountId` | Restrict |

## Owned types

- `Discount`: `DiscountValue` (Money), `MinOrderAmount?` (Money), `MaxDiscountAmount?` (Money),
  `ValidityPeriod` (DateRange → `ValidFrom`/`ValidTo`).
- `DiscountUsage`: `Amount` (Money).

## Cross-module logical references (no DB FK)

- `UserId`, `ProviderId`, `TourId`, `CategoryId`, `BusinessId`, `ServiceItemId`, `EntityId`,
  `BookingId`, admin IDs (see central catalog).

## Notes / unclear

- `CommissionRule.Tier` is free-text (no entity reference).
- `ProviderBankAccount`/`ProviderPaymentMethod` are aggregate roots keyed by provider `UserId`.
