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
