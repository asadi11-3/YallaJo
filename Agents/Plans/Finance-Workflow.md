# Finance Workflow Plan

> **Module**: Finance  
> **Dependencies**: Booking (integration events), Accounts (AgencyAffiliation), ContentTours (tour data)  
> **Compatible With**: Booking-Workflow.md, TourGuide-Flow.md, Platform-Onboarding-Workflow.md, Role-System.md  
> **Status**: Plan — Not yet executed

---

## Design Decisions (12 — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | Finance owns ALL money | GuidePaymentMethod/GuidePayout/GuideEarning → Finance module. ContentTours only stores reference IDs. |
| 2 | Revenue-tier CommissionRule only | Keep existing CommissionRule (tier by monthly revenue). GuideTrustTier unlocks NON-financial perks only (auto-approval, search priority, more slots). |
| 3 | Platform splits agency bookings | Two payouts per agency booking: one for agency, one for guide. |
| 4 | Admin-only dispute resolution | Tourist files → Admin reviews → Admin decides (full/partial/no refund). Provider submits response but admin has final say. |
| 5 | Defer Loyalty & Referral | Post-MVP. Shell entities remain untouched. |
| 6 | Defer Subscription | Post-MVP. Shell entities remain untouched. |
| 7 | Unified PaymentMethod entity | Finance owns. Supports BankTransfer + JoMoPay + OrangeMoney + ZainCash. Replaces ProviderBankAccount shell. |
| 8 | Two Payouts per agency booking | Separate PayoutItem/batch for agency and guide. Each accumulates independently. |
| 9 | Agency % of post-platform remainder | Example: 100 JOD booking → 15% platform (15 JOD) → remaining 85 JOD → agency 20% (17 JOD) + guide 80% (68 JOD). |
| 10 | IndependentGuide: full post-commission | No split. 100 JOD - 15% platform = 85 JOD → all to guide. Single payout. |
| 11 | Auth gate fix in Phase 1 | Fix 8 handlers with ICurrentUser auth gate violations. |
| 12 | Full invoicing | Payment Invoice + Credit Note (for refunds) + Monthly Statement (for providers). |

---

## Current State (What's Already Built)

### ✅ Fully Implemented (Keep As-Is)
- **Payment** (455L, AggregateRoot): Full state machine, gateway integration, escrow tracking
- **PaymentExpectation** (87L): Booking→Finance bridge. Seeded by TourBookingCreatedIntegrationEvent
- **Payout** (241L, AggregateRoot): Batch creation, approval, hold, completion
- **Invoice** (170L, AggregateRoot): Auto-generated on payment completion, PDF rendering
- **CommissionRule** (118L): Tier-based by monthly revenue. CRUD + lookup
- **FakePaymentGateway**: Deterministic test gateway (will be replaced by Stripe adapter)
- **PayoutBatchingService**: Weekly (Sunday midnight UTC)
- **RefundRetryService**: Every 15min, max 3 retries
- **Webhook Processing**: HMAC verification, idempotent inbox pattern
- **Event Handlers**: BookingCreated→PaymentExpectation, BookingCompleted→Escrow, BookingCancelled→Refund, PaymentCompleted→Invoice
- **Endpoints**: Payment (6), Payout (5), CommissionRule (4), Invoice (2-3) = 21 total

### ❌ Shell Only (Needs Full Implementation)
- **Dispute** (25L): Properties only → needs full state machine
- **ProviderBankAccount** (18L): Properties only → REPLACED by unified PaymentMethod

### ❌ Not Yet Built
- Unified PaymentMethod entity (bank + mobile wallets)
- Agency split logic in payout batching
- Dispute lifecycle (file, respond, resolve)
- Credit Note entity (for refunds)
- Monthly Statement generation
- GuideEarning tracking (per-booking financial record)
- Auth gate cleanup (8 handlers)
- Runtime throw fixes in domain entities
- Provider/Guide payout dashboard endpoints
- AdminFinanceDashboard endpoints

### 🚫 Deferred (Post-MVP, Shell Stays)
- Discount (entity lives in Finance, CRUD deferred — discount EVALUATION lives in Booking)
- LoyaltyPoints / LoyaltyTransaction
- Referral
- Subscription / SubscriptionPlan / SubscriptionFeature / PlanFeature

---

## Entity Design

### New: PaymentMethod (replaces ProviderBankAccount shell)

```csharp
public sealed class PaymentMethod : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public PaymentMethodType Type { get; private set; }  // BankTransfer=0, JoMoPay=1, OrangeMoney=2, ZainCash=3
    public string Label { get; private set; }            // User-friendly name ("My Savings", "Orange Mobile")
    public string Currency { get; private set; }         // JOD, USD, EUR
    public bool IsDefault { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    
    // Bank-specific (nullable when Type != BankTransfer)
    public string? BankName { get; private set; }
    public string? AccountHolderName { get; private set; }
    public string? AccountNumber { get; private set; }
    public string? Iban { get; private set; }
    public string? SwiftCode { get; private set; }
    
    // Mobile wallet-specific (nullable when Type == BankTransfer)
    public string? PhoneNumber { get; private set; }
    public string? WalletAccountId { get; private set; }
    
    // Methods
    public static PaymentMethod RegisterBank(...) { }
    public static PaymentMethod RegisterMobileWallet(...) { }
    public Result Update(...) { }
    public Result SetAsDefault() { }
    public Result Verify(DateTime verifiedAt) { }
    public Result Deactivate() { }
}
```

### New: PaymentMethodType Enum

```csharp
public enum PaymentMethodType
{
    BankTransfer = 0,
    JoMoPay = 1,
    OrangeMoney = 2,
    ZainCash = 3
}
```

### Modified: Dispute (shell → full state machine)

```csharp
public sealed class Dispute : AuditableEntity, IAggregateRoot
{
    public Guid PaymentId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid FiledByUserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public DisputeStatus Status { get; private set; }
    public string Reason { get; private set; }
    public string? ProviderResponse { get; private set; }
    public DateTime? ProviderRespondedAt { get; private set; }
    public DisputeResolution? Resolution { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public Guid? ResolvedByAdminId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public decimal? RefundAmount { get; private set; }      // When Resolution = RefundPartial
    public DateTime FiledAt { get; private set; }
    public DateTime Deadline { get; private set; }          // FiledAt + 7 days
    
    // Collections
    public ICollection<DisputeEvidence> Evidences { get; private set; }
    public ICollection<DisputeMessage> Messages { get; private set; }
    
    // State machine methods
    public static Result<Dispute> File(paymentId, bookingId, userId, providerId, reason, DateTime utcNow) { }
    public Result SubmitProviderResponse(string response, DateTime utcNow) { }
    public Result MoveToUnderReview(Guid adminId) { }
    public Result Resolve(DisputeResolution resolution, string notes, Guid adminId, decimal? refundAmount, DateTime utcNow) { }
    public Result Escalate(Guid adminId, string reason) { }
    public Result Close(Guid adminId, string reason) { }
}
```

**DisputeStatus**: Open=0, UnderReview=1, Resolved=2, Escalated=3, Closed=4  
**DisputeResolution**: RefundFull=0, RefundPartial=1, NoRefund=2, Compromise=3

### New: CreditNote (for refund documentation)

```csharp
public sealed class CreditNote : AuditableEntity, IAggregateRoot
{
    public Guid InvoiceId { get; private set; }         // Original invoice being credited
    public Guid RefundPaymentId { get; private set; }   // The refund payment
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public string CreditNoteNumber { get; private set; } // CN-YYYYMM-XXXXXX
    public Money Amount { get; private set; }
    public string Currency { get; private set; }
    public string Reason { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public string? PdfStoragePath { get; private set; }
    
    public static CreditNote Generate(invoice, refundPayment, creditNoteNumber, reason, timeProvider) { }
    public void MarkPdfRendered(string path) { }
}
```

### MonthlyStatement — DROPPED (no table, DTO + on-demand PDF only)

> **UNMAPPED DECISION:** No MonthlyStatement table. All data computed from Payment + Payout + GuideEarning.
> PDF generated on-demand (or pre-rendered by background service → blob storage with convention path).

```csharp
// DTO only — never persisted to a table
public sealed record MonthlyStatementDto(
    Guid ProviderId,
    int Year,
    int Month,
    string Currency,
    decimal GrossRevenue,
    decimal TotalCommission,
    decimal TotalRefunds,
    decimal NetRevenue,
    decimal TotalPaidOut,
    decimal PendingPayout,
    int BookingCount,
    int RefundCount,
    int DisputeCount);

// Endpoint: GET /finance/statements/{year}/{month}
// 1. Query Payment/Payout/GuideEarning for provider + period → MonthlyStatementDto
// 2. Return JSON (or render PDF via QuestPDF on GET /finance/statements/{year}/{month}/pdf)
// PDF blob path convention: statements/{providerId}/{year}/{month}.pdf
// Background service checks if PDF exists → generates if missing (1st of month, 02:00 UTC)
```

No entity, no EF config, no repository, no migration.

### New: GuideEarning (per-booking financial record)

```csharp
public sealed class GuideEarning : AuditableEntity
{
    public Guid GuideUserId { get; private set; }       // TourGuide.UserId
    public Guid BookingId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid TourId { get; private set; }
    public Guid? AgencyId { get; private set; }         // NULL for independent guides
    public string Currency { get; private set; }
    public Money GrossAmount { get; private set; }      // Full booking amount
    public Money PlatformCommission { get; private set; }
    public Money AgencyCut { get; private set; }        // 0 for independent guides
    public Money NetEarning { get; private set; }       // What guide actually receives
    public EarningStatus Status { get; private set; }   // Pending=0, InEscrow=1, Released=2, Disputed=3, Refunded=4
    public DateTime EarnedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public Guid? PayoutId { get; private set; }         // Linked when included in a payout batch
    
    public static GuideEarning Create(guideUserId, bookingId, paymentId, tourId, agencyId?, grossAmount, platformCommission, agencyCut, timeProvider) { }
    public Result MarkInEscrow(DateTime escrowStart) { }
    public Result MarkReleased(Guid payoutId, DateTime releasedAt) { }
    public Result MarkDisputed() { }
    public Result MarkRefunded() { }
}
```

### New: EarningStatus Enum

```csharp
public enum EarningStatus
{
    Pending = 0,
    InEscrow = 1,
    Released = 2,
    Disputed = 3,
    Refunded = 4
}
```

---

## Agency Split Logic (Decision #3, #8, #9, #10)

### Split Calculation Flow (in TriggerPayoutCommandHandler)

```
1. Get escrow-eligible payments (EscrowReleaseEligibleAt <= now)
2. For each payment:
   a. Lookup booking → get GuideId, ProviderId (agency)
   b. If ProviderId == GuideId (IndependentGuide):
      → Single recipient: guide gets (GrossAmount - PlatformCommission)
      → One PayoutItem for guide only
   c. If ProviderId != GuideId (Agency booking):
      → Lookup AgencyAffiliation.CommissionPercentage (from Accounts.Contracts)
      → PostPlatform = GrossAmount - PlatformCommission
      → AgencyCut = PostPlatform * AgencyAffiliation.CommissionPercentage / 100
      → GuideCut = PostPlatform - AgencyCut
      → Two PayoutItems: one for Agency's payout batch, one for Guide's payout batch
3. Group PayoutItems by (RecipientUserId, Currency)
4. Create/append to Payout batches per recipient
```

### New Contract: IAgencySplitReader

```csharp
// In Accounts.Contracts/Abstractions/
public interface IAgencySplitReader
{
    Task<AgencySplitInfo?> GetSplitInfoAsync(Guid guideUserId, Guid agencyProviderId, CancellationToken ct);
}

public sealed record AgencySplitInfo(
    Guid AgencyUserId,      // Agency owner's UserId (payout recipient)
    decimal CommissionPct   // Agency's % of post-platform remainder
);
```

### Modified: Payout Entity

Add to Payout:
```csharp
public PayoutRecipientType RecipientType { get; private set; }  // Provider=0, Guide=1
```

### Modified: PayoutItem

Add to PayoutItem:
```csharp
public Guid? GuideUserId { get; private set; }   // Set when RecipientType == Guide
public Guid? AgencyUserId { get; private set; }  // Set when this is an agency split
```

---

## Dispute Workflow (Decision #4)

```
State Machine: Open → UnderReview → Resolved / Escalated → Closed

Flow:
1. Tourist files dispute within 7 days of tour completion
   - Validation: only 1 dispute per booking, payment must be Completed, within 7-day window
   - Creates Dispute (Open status)
   - Fires DisputeFiledDomainEvent → notification to provider + admin
   
2. Provider responds (optional, within 48h)
   - Adds ProviderResponse + evidence/messages
   - Status stays Open (admin hasn't picked it up yet)
   
3. Admin picks up → UnderReview
   - Admin assigned, can request more evidence from either party
   
4. Admin resolves:
   - RefundFull: creates full refund Payment, fires DisputeResolvedDomainEvent
   - RefundPartial: creates partial refund Payment with specified amount
   - NoRefund: closes in favor of provider
   - Compromise: custom amount, notes explaining decision
   
5. If complex → Escalate (to SuperAdmin)
   - SuperAdmin has final authority

6. After resolution → Closed (after any refund is processed)
```

### Dispute Endpoints (8)

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | /disputes | User | File a dispute (bookingId, reason, evidence?) |
| GET | /disputes/{id} | User/Admin | Get dispute detail |
| GET | /disputes/my | User | My filed disputes |
| POST | /disputes/{id}/respond | Provider | Submit provider response |
| POST | /disputes/{id}/evidence | User/Provider | Add evidence (attachment URL) |
| GET | /admin/disputes | Admin | Queue (filterable by status) |
| POST | /admin/disputes/{id}/review | Admin | Move to UnderReview |
| POST | /admin/disputes/{id}/resolve | Admin | Resolve with decision |

---

## PaymentMethod Endpoints (Decision #7)

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | /payment-methods | Provider/Guide | List my payment methods |
| POST | /payment-methods/bank | Provider/Guide | Register bank account |
| POST | /payment-methods/wallet | Provider/Guide | Register mobile wallet |
| PUT | /payment-methods/{id} | Provider/Guide | Update method details |
| PATCH | /payment-methods/{id}/default | Provider/Guide | Set as default |
| DELETE | /payment-methods/{id} | Provider/Guide | Deactivate (soft) |
| POST | /admin/payment-methods/{id}/verify | Admin | Mark as verified |

---

## Invoicing & Statements (Decision #12)

### Credit Note Flow
1. Refund Payment completes (MarkRefundCompleted)
2. Domain event: RefundCompletedDomainEvent
3. Event handler: `OnRefundCompletedGenerateCreditNoteHandler`
   - Finds original Invoice by PaymentId
   - Generates CreditNote with next sequence number (CN-YYYYMM-XXXXXX)
   - Cancels original Invoice (CancelDueToRefund)
   - Renders PDF

### Monthly Statement Flow
1. Background service: `MonthlyStatementGenerationService`
   - Runs 1st of each month, 02:00 UTC
   - For each active provider with completed bookings in previous month:
     - Aggregates: gross revenue, commissions, refunds, net, paid out, pending
     - Computes MonthlyStatementDto from Payment/Payout/GuideEarning
     - Renders PDF
     - Fires MonthlyStatementGeneratedIntegrationEvent → Messaging (email notification)

### Statement Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | /statements/my | Provider/Guide | My monthly statements (paginated) |
| GET | /statements/{id} | Provider/Guide | Statement detail |
| GET | /statements/{id}/pdf | Provider/Guide | Download PDF |
| GET | /admin/statements | Admin | All statements (filterable) |

---

## Guide Earnings Dashboard (Decision #1)

### Endpoints (from TourGuide-Flow.md, served by Finance module)

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | /guides/me/earnings/summary | TourGuide | Total/month/week summary |
| GET | /guides/me/earnings | TourGuide | Per-booking earnings list (paginated) |
| GET | /guides/me/earnings/{id} | TourGuide | Single earning detail |
| GET | /guides/me/payouts | TourGuide | My payout history |
| GET | /guides/me/payouts/{id} | TourGuide | Payout detail with items |

Note: These endpoints live in Finance.Presentation but route under `/guides/me/` for UX consistency. They query GuideEarning + Payout filtered by `GuideUserId == currentUser.UserId`.

---

## Admin Finance Dashboard (Decision #11 partial)

### Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | /admin/finance/overview | Admin | Platform revenue summary (today/week/month/all-time) |
| GET | /admin/finance/revenue | Admin | Revenue breakdown by provider/tour/period |
| GET | /admin/finance/commissions | Admin | Commission collected (aggregated) |
| GET | /admin/finance/disputes | Admin | Dispute summary (open/resolved/escalated counts) |
| POST | /admin/finance/export | Admin | Export financial data (CSV/PDF) |

---

## Auth Gate Cleanup (Decision #11)

**8 handlers with inline auth checks** (same violation as ContentPlaces/ContentBlogs/ContentTours Gap #1):

Finance.Application command handlers:
1. InitiatePaymentCommandHandler
2. RefundPaymentCommandHandler

Finance.Presentation inline handlers (endpoint delegates, not separate handler classes):
3. GetPaymentById
4. GetMyPayments
5. GetAllPayments (admin)
6. GetProviderPayouts
7. GetPayoutById
8. ApprovePayoutCommand handler

**Fix**: Remove `if (!currentUser.IsAuthenticated || currentUser.UserId is null)` blocks. Auth is guaranteed at endpoint level by `MustHavePermission`. Change `.UserId.Value` → `.UserId!.Value`.

---

## Runtime Throw Fixes

Domain entities using `throw` instead of Result pattern (violates agent-context.md §2.3):

1. **Payment.cs** — Multiple `throw new InvalidOperationException` in state transitions → return `Result.Failure`
2. **Payout.cs** — Same pattern in AddItem, PutOnHold, Approve, MarkCompleted
3. **CommissionRule.cs** — Deactivate throws on already inactive
4. **PaymentExpectation.cs** — MarkPaid/Cancel throw on invalid state

**Note**: Keep factory method throws (they're valid fail-fast at creation). Only convert state-transition throws to Result.

---

## Background Services (New + Modified)

| Service | Schedule | Purpose |
|---------|----------|---------|
| PayoutBatchingService | Weekly (Sun midnight) | ✅ EXISTS — modify for agency split |
| RefundRetryService | Every 15min | ✅ EXISTS — no changes |
| MonthlyStatementGenerationService | 1st of month, 02:00 | NEW — pre-render PDF to blob (no DB table) |
| DisputeDeadlineService | Daily | NEW — escalate disputes past 7-day deadline |

---

## Integration Events (New)

### Outbound (Finance → other modules)
- `DisputeFiledIntegrationEvent(DisputeId, BookingId, UserId, ProviderId, Reason)`
- `DisputeResolvedIntegrationEvent(DisputeId, Resolution, RefundAmount?)`
- `PayoutCompletedIntegrationEvent` ✅ EXISTS
- `MonthlyStatementGeneratedIntegrationEvent(ProviderId, Year, Month, PdfPath)`
- `CreditNoteIssuedIntegrationEvent(CreditNoteId, InvoiceId, UserId, Amount)`

### Inbound (other modules → Finance)
- `TourBookingCreatedIntegrationEvent` ✅ HANDLED
- `TourBookingCompletedIntegrationEvent` ✅ HANDLED
- `TourBookingCancelledIntegrationEvent` ✅ HANDLED
- `TourBookingPaymentExpiredIntegrationEvent` ✅ HANDLED
- `ProviderSuspendedIntegrationEvent` ✅ HANDLED (holds payouts)

### New Inbound (from Booking, after Booking-Workflow is built)
- `BookingGuideAssignedIntegrationEvent(BookingId, GuideUserId, ProviderId, IsAgency)` → Finance creates GuideEarning record

---

## Execution Phases

### Phase 1: Auth Gate Cleanup (Gap fix)
- Fix 8 handlers/endpoints with ICurrentUser auth gate violations
- ~8 files modified
- Build + verify

### Phase 2: Unified PaymentMethod Entity
- Create PaymentMethod entity (replaces ProviderBankAccount)
- Create PaymentMethodType enum
- EF Configuration + migration considerations
- Create repository interface + implementation
- Create handlers: RegisterBank, RegisterWallet, Update, SetDefault, Deactivate, AdminVerify
- Create endpoints (7)
- Remove/deprecate ProviderBankAccount references
- Update TriggerPayoutCommandHandler to use new PaymentMethod
- ~20-25 new files, ~5 modified

### Phase 3: Agency Split Logic
- Create IAgencySplitReader contract in Accounts.Contracts
- Implement in Accounts.Application
- Modify TriggerPayoutCommandHandler for dual-payout logic
- Add PayoutRecipientType enum + field on Payout
- Add GuideUserId/AgencyUserId to PayoutItem
- Create GuideEarning entity + repo + EF config
- Create EarningStatus enum
- Modify PayoutBatchingService to create GuideEarning records
- ~15-18 new files, ~8 modified

### Phase 4: Dispute Lifecycle
- Rewrite Dispute entity (shell → full state machine with Result pattern)
- Add domain events: DisputeFiledDomainEvent, DisputeResolvedDomainEvent
- Create DisputeEvidence/DisputeMessage with proper methods
- Create handlers: FileDispute, SubmitResponse, AddEvidence, AdminReview, AdminResolve
- Create queries: GetDispute, GetMyDisputes, GetAdminDisputeQueue
- Create endpoints (8)
- Create DisputeDeadlineService (background)
- Create integration event handlers (notification triggers)
- ~25-30 new files, ~5 modified

### Phase 5: Credit Note + Monthly Statement
- Create CreditNote entity + repo + EF config
- Create MonthlyStatementDto + query handler (computes from Payment/Payout/GuideEarning, no table)
- Create OnRefundCompletedGenerateCreditNoteHandler (domain event handler)
- Create MonthlyStatementGenerationService (background)
- Create QuestPDF templates for CreditNote + Statement
- Create endpoints (4 statement endpoints)
- Integration events for notifications
- ~18-22 new files, ~3 modified

### Phase 6: Guide Earnings Dashboard
- Create GuideEarning queries: GetSummary, GetEarnings (paginated), GetEarningById
- Create endpoint group: `/guides/me/earnings/*`, `/guides/me/payouts/*`
- ~10-12 new files

### Phase 7: Runtime Throw Fixes
- Convert throws to Result in: Payment, Payout, CommissionRule, PaymentExpectation
- Update all callers to handle Result
- ~4-6 files modified

### Phase 8: Admin Finance Dashboard
- Create overview/revenue/commission/dispute aggregate queries
- Create export command (CSV + PDF)
- Create endpoints (5)
- ~12-15 new files

### Phase 9: Solution Build + Verify
- Build entire solution
- Fix any test compilation errors
- Verify 0 errors

---

## File Count Estimate

| Category | New Files | Modified Files |
|----------|-----------|----------------|
| Phase 1: Auth gate | 0 | 8 |
| Phase 2: PaymentMethod | 20-25 | 5 |
| Phase 3: Agency split | 15-18 | 8 |
| Phase 4: Dispute | 25-30 | 5 |
| Phase 5: Credit Note + Statement | 18-22 | 3 |
| Phase 6: Guide earnings dashboard | 10-12 | 0 |
| Phase 7: Runtime throws | 0 | 6 |
| Phase 8: Admin dashboard | 12-15 | 0 |
| **Total** | **~100-130** | **~35** |

---

## Cross-Module Compatibility Matrix

| This Plan Depends On | What It Needs |
|---------------------|---------------|
| Booking-Workflow.md | BookingGuideAssignedIntegrationEvent for GuideEarning. IPaymentGateway shared contract. |
| TourGuide-Flow.md | GuideUserId for earnings tracking. Guide trust tier does NOT affect commission (Decision #2). |
| Platform-Onboarding-Workflow.md | AgencyAffiliation.CommissionPercentage for split calculation. IAgencySplitReader contract. |
| Role-System.md | Provider/TourGuide roles required for PaymentMethod + Earnings endpoints. |

| Other Plans Depend On This | What They Need |
|---------------------------|---------------|
| Booking-Workflow.md | PaymentExpectation seeded on booking creation. Payment gateway execution. |
| TourGuide-Flow.md | `/guides/me/earnings/*` and `/guides/me/payouts/*` endpoints served by Finance. |
| Platform-Onboarding-Workflow.md | PaymentMethod CRUD for provider dashboard. |

---

## Deferred Items (Post-MVP — Shell Entities Unchanged)

- **Discount**: Entity stays in Finance, evaluation logic in Booking. No CRUD endpoints built.
- **LoyaltyPoints + LoyaltyTransaction**: Full gamification system. Entity shells preserved.
- **Referral**: Referral code system. Entity shell preserved.
- **Subscription + SubscriptionPlan + SubscriptionFeature**: Tiered platform access. Entity shells preserved.
- **DiscountUsage**: Tracking per-user discount redemptions. Shell preserved.

---

## Notes

1. **Payout Gateway Execution**: Currently `TriggerPayoutCommandHandler` creates batches but does NOT call `gateway.PayoutAsync`. A future Phase (or admin manual action via `ApprovePayoutCommand`) should trigger actual disbursement.
2. **Stripe Adapter**: FakePaymentGateway → real Stripe implementation is infrastructure work, not domain. IPaymentGateway interface is stable.
3. **Currency**: Platform operates in JOD, USD, EUR (matches Tour.AllowedCurrencies). Payouts in provider's preferred currency.
4. **Min Payout Threshold**: 20 JOD (from TourGuide-Flow.md) for guides. 10 JOD (from Business Rules PDF) for tour providers. Configurable per PaymentMethodType.
5. **Escrow Period**: 7 days from tour completion (EscrowOptions.HoldPeriodDays). Configurable.
6. **CommissionLookupService**: Falls back to 15%/JOD if no active rule matches. Future: different default rates by ProviderType.
