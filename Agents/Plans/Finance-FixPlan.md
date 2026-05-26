# Finance Module — Fix Plan

> **Created**: 2025-01-27
> **Based on**: `Finance-Audit-Report.md`
> **Effort**: ~3-4 hours (doc fixes + minor code)
> **Build gate**: Fix 12

---

## Design Decisions

1. Plan document corrections are priority — prevents downstream confusion
2. ProviderBankAccount removal requires migration — mark as follow-up only
3. Discount entity fleshing out deferred to separate work
4. CreditNote and GuideEarning entity design decisions NOT in scope — require business input
5. IPaymentGateway signatures are correct as-is — plan was aspirational

---

## Fixes

### Fix 1 (HIGH) — Update plan doc endpoint count
- **File**: `Finance-Workflow.md`
- **Change**: "21 endpoints" → "33 endpoints" with full breakdown

### Fix 2 (HIGH) — Correct Dispute entity status
- **File**: `Finance-Workflow.md`
- **Change**: "Dispute(25L) SHELL" → "Dispute(124L) FULLY IMPLEMENTED — aggregate root with state machine"

### Fix 3 (MEDIUM) — Update PaymentMethodType enum
- **File**: `Finance-Workflow.md`
- **Change**: Document actual enum name `ProviderPaymentMethodType` with values: BankAccount=0, JoMoPay=1, OrangeMoney=2, ZainCash=3 (not BankTransfer)

### Fix 4 (MEDIUM) — Update permission catalog
- **File**: `Finance-Workflow.md`
- **Change**: "7 features" → "8 features (Payment, Refund, Invoice, Payout, CommissionRule, ProviderBankAccount, ProviderPaymentMethod, AdminFinanceDashboard), 27 descriptors"

### Fix 5 (MEDIUM) — Update IPaymentGateway spec
- **File**: `Finance-Workflow.md`
- **Change**: Document actual interface: `GatewayName` + `InitiateAsync`, `VerifyWebhookSignatureAsync`, `RefundAsync`, `PayoutAsync`

### Fix 6 (MEDIUM) — Document ProviderPaymentMethod as active model
- **File**: `Finance-Workflow.md`
- **Change**: Add section explaining ProviderPaymentMethod exists (not PaymentMethod), coexists with ProviderBankAccount, is the active payout destination model

### Fix 7 (LOW) — Fix line counts
- **File**: `Finance-Workflow.md`
- **Changes**: Payment 455→485, Payout 241→269, Invoice 170→177

### Fix 8 (LOW) — Update plan status
- **File**: `Finance-Workflow.md`
- **Change**: Mark as "Partially Implemented (audited 2025-01-27)" with implementation notes

### Fix 9 (LOW) — Document existing background services + event handlers
- **File**: `Finance-Workflow.md`
- **Change**: Add inventory: 2 bg services, 5 inbound consumers, 10 outbound converters, 1 domain handler

### Fix 10 (LOW) — Update DisputeEvidence/Message status
- **File**: `Finance-Workflow.md`
- **Change**: Mark as EXISTS (not NOT BUILT)

### Fix 11 (LOW) — Document FinancePermissionCatalog stale comment
- **File**: `Finance.Contracts/Authorization/FinancePermissionCatalog.cs`
- **Change**: Update internal comment to reflect actual 8 features, 27 descriptors

### Fix 12 (LOW) — Build verify
- No code changes expected from doc-only fixes; Fix 11 is comment-only

---

## Execution Order

All fixes are independent. Can be done in single pass over Finance-Workflow.md + one comment fix.

---

## NOT In Scope (Deferred to Business Decisions)

1. Remove ProviderBankAccount → requires migration + data migration plan
2. CreditNote entity design — needs business requirements
3. GuideEarning entity design — query-level already works via EarningHandlers
4. Discount entity fleshing out — needs product decision
5. Agency payout split logic — blocked on booking/provider integration
6. Subscription/Loyalty/Referral — explicitly deferred in `_Deferred/`
