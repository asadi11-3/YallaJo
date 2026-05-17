# TASK 2 — Webhook + Refund + Payment Reads

> **Owner:** Mahmoud — **Hours:** 32h — **Hard deadline:** Sun **2026-09-27 17:00**
> **Earliest start:** Mon 2026-09-14 (after T1)
> **Endpoints:** 5 (webhook, refund, GET /payments/{id}, GET /my-payments, GET /admin/all)
> **Depends on:** T1 (Payment aggregate factory)

---

## 1. Endpoint Surface

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/payments/webhook` | `.AllowAnonymous()` | HMAC-verified. **F-R2.** |
| 2 | POST | `/api/v1/payments/{id}/refund` | `Refund.Create` | Admin or self/provider with cancel reason |
| 3 | GET | `/api/v1/payments/{id}` | `Payment.Read` | Self/provider/admin ownership |
| 4 | GET | `/api/v1/payments/my-payments` | `Payment.Read` | Self-filter cursor |
| 5 | GET | `/api/v1/payments/admin/all` | `AdminFinanceDashboard.Read` | Filters: userId, providerId, status, dateRange, currency |

Plus **1 inbox handler** `BookingTourBookingCancelledHandler` consumes `booking.tour-booking.cancelled.v1` → auto-creates Refund per snapshot policy `RefundPercentage × Payment.AmountTotal`.

---

## 2. POST /payments/webhook

### Request shape (gateway-agnostic envelope)
```http
POST /api/v1/payments/webhook
X-Signature: hmac-sha256=<hex>
X-Gateway: Fake               # or "Stripe", "HyperPay"
Content-Type: application/json

{
  "eventId": "evt_abc123",
  "eventType": "payment.succeeded",     // or "payment.failed", "refund.succeeded", "refund.failed"
  "occurredAt": "2026-09-01T10:14:32Z",
  "data": {
    "gatewayPaymentId": "gw-pay-...",
    "amount": 120.00,
    "currency": "JOD",
    "failureCode": null,
    "failureMessage": null
  }
}
```

### Handler flow
```csharp
public async Task<Result> Handle(WebhookCommand cmd, CancellationToken ct)
{
    // F-R2: verify signature against raw body BEFORE we even parse it.
    var verified = await _gateway.VerifyWebhookSignatureAsync(cmd.RawBody, cmd.Headers, ct);
    if (!verified)
        return Result.Failure(new Error("Payment.WebhookSignatureMismatch", "HMAC mismatch"), Outcome.Forbidden);

    // F-R3: idempotency — short-circuit on duplicate event delivery
    if (await _webhookInbox.HasBeenProcessedAsync(cmd.EventId, ct))
        return Result.Success();  // 200 noop

    // dispatch by event type
    switch (cmd.EventType)
    {
        case "payment.succeeded":
            await HandlePaymentSucceededAsync(cmd, ct);
            break;
        case "payment.failed":
            await HandlePaymentFailedAsync(cmd, ct);
            break;
        case "refund.succeeded":
            await HandleRefundSucceededAsync(cmd, ct);
            break;
        case "refund.failed":
            await HandleRefundFailedAsync(cmd, ct);
            break;
        default:
            _logger.LogWarning("Unknown webhook event type {Type}", cmd.EventType);
            // still record as processed so retries don't spam
            break;
    }

    _webhookInbox.MarkAsProcessed(cmd.EventId);
    await _uow.SaveChangesAsync(ct);
    return Result.Success();
}
```

### `HandlePaymentSucceededAsync`
```csharp
private async Task HandlePaymentSucceededAsync(WebhookCommand cmd, CancellationToken ct)
{
    var payment = await _paymentRepo.GetByGatewayTransactionIdAsync(cmd.GatewayPaymentId, ct);
    if (payment is null)
    {
        _logger.LogError("Webhook for unknown gw payment {GwId}", cmd.GatewayPaymentId);
        return;  // 200 — gateway will stop retrying if we return 2xx
    }

    // State guard: only Pending payments can transition to Completed
    var result = payment.MarkCompleted(cmd.GatewayTransactionId, _timeProvider.GetUtcNow().UtcDateTime);
    if (result.IsFailure) { _logger.LogWarning("MarkCompleted failed: {Code}", result.Error?.Code); return; }
    // Raises PaymentCompletedDomainEvent → outbox `finance.payment.completed.v1` (consumed by Booking inbox to confirm booking)
    // → Invoice generation also kicked off via T3 InvoiceGeneratorOnPaymentCompleted domain event handler

    await _cache.RemoveByTagAsync($"payment:{payment.Id}", ct);
    await _cache.RemoveByTagAsync($"payments:user:{payment.UserId}", ct);
}
```

### `HandlePaymentFailedAsync`
Marks Payment as Failed, raises `PaymentFailedDomainEvent` → `finance.payment.failed.v1` → Booking inbox releases SlotLock + restores capacity.

### `HandleRefundSucceededAsync` / `HandleRefundFailedAsync`
Look up Refund-typed Payment row by GatewayRefundId. Mark Completed / Failed. Raise corresponding events.

---

## 3. POST /payments/{id}/refund

### Request
```http
POST /api/v1/payments/01956f6a.../refund
Authorization: Bearer <jwt>

{
  "amount": 60.00,
  "currency": "JOD",
  "reason": "UserCancellation",         // RefundReason enum
  "notes": "Free text up to 500 chars"
}
```

### Handler flow
```csharp
public async Task<Result<RefundResponse>> Handle(RefundPaymentCommand cmd, CancellationToken ct)
{
    var original = await _paymentRepo.GetByIdAsync(cmd.PaymentId, ct);
    if (original is null) return Result.Failure<RefundResponse>(new Error("Payment.NotFound", ""), Outcome.NotFound);
    if (original.Status is not PaymentStatus.Completed)
        return Result.Failure<RefundResponse>(new Error("Refund.PaymentNotCompleted", ""), Outcome.Conflict);

    // ownership / role-based check
    if (!_currentUser.IsInRole("Admin") && original.UserId != _currentUser.UserId && original.ProviderId != _currentUser.ProviderId)
        return Result.Failure<RefundResponse>(new Error("Payment.OwnerMismatch", ""), Outcome.Forbidden);

    if (cmd.Currency != original.AmountTotal.Currency)
        return Result.Failure<RefundResponse>(new Error("Refund.CurrencyMismatch", ""), Outcome.Validation);

    var refundable = original.AmountTotal.Amount - original.RefundedTotal;
    if (cmd.Amount <= 0 || cmd.Amount > refundable)
        return Result.Failure<RefundResponse>(new Error("Refund.AmountExceedsRefundable", $"Refundable left: {refundable}"), Outcome.Validation);

    // Create a sibling Payment row with Type=Refund (negative amount tracked via Type discriminator, value stored absolute)
    var refund = original.CreateRefund(amount: cmd.Amount, reason: cmd.Reason, notes: cmd.Notes, timeProvider: _timeProvider);
    // Raises RefundInitiatedDomainEvent → outbox finance.refund.initiated.v1

    // Call gateway
    RefundResult gw;
    try { gw = await _gateway.RefundAsync(new RefundRequest(original.GatewayTransactionId, cmd.Amount, cmd.Currency, cmd.Reason.ToString()), ct); }
    catch (Exception ex) { _logger.LogError(ex, "Refund gateway error"); refund.MarkFailed("GatewayError"); }

    if (gw is { Status: RefundStatus.Completed })
        refund.MarkCompleted(gw.GatewayRefundId, _timeProvider.GetUtcNow().UtcDateTime);
    else if (gw is { Status: RefundStatus.Pending })
        { /* leave pending; webhook will move it forward */ }
    else
        refund.MarkFailed(gw?.FailureCode ?? "Unknown");

    await _paymentRepo.AddAsync(refund, ct);
    await _uow.SaveChangesAsync(ct);
    await _cache.RemoveByTagAsync($"payment:{cmd.PaymentId}", ct);
    return Result.Success(new RefundResponse(refund.Id, refund.Status, gw?.GatewayRefundId));
}
```

### Domain methods on `Payment` (T2 deliverables — additive to T1)
```csharp
public Result MarkCompleted(string gatewayTxId, DateTime occurredAtUtc);
public Result MarkFailed(string reasonCode, string? rawMessage);
public Payment CreateRefund(decimal amount, RefundReason reason, string? notes, TimeProvider timeProvider);   // factory for sibling row
public Result MarkRefundCompleted(string gatewayRefundId, DateTime occurredAtUtc);
public Result MarkRefundFailed(string failureCode);
public decimal RefundedTotal { get; private set; }   // updated on each refund.complete
```

---

## 4. Inbox handler — `BookingTourBookingCancelledHandler`

```csharp
internal sealed class BookingTourBookingCancelledHandler(...) : INotificationHandler<IntegrationEventEnvelope<BookingTourBookingCancelledIntegrationEvent>>
{
    public async Task Handle(IntegrationEventEnvelope<BookingTourBookingCancelledIntegrationEvent> e, CancellationToken ct)
    {
        if (await _inbox.HasBeenProcessedAsync(e.EventId, ct)) return;
        var ev = e.Payload;
        if (ev.RefundPercentage <= 0m)
        {
            _logger.LogInformation("Cancellation {BookingId} has 0% refund — no Refund row created", ev.BookingId);
            _inbox.MarkAsProcessed(e.EventId);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        var payment = await _paymentRepo.GetActiveByBookingIdAsync(ev.BookingId, ct);
        if (payment is null || payment.Status != PaymentStatus.Completed)
        {
            _logger.LogWarning("Cancellation refund cannot proceed — payment not completed");
            _inbox.MarkAsProcessed(e.EventId);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        var refundAmount = Math.Round(payment.AmountTotal.Amount * (ev.RefundPercentage / 100m), 2, MidpointRounding.ToEven);
        var reason = ev.ProviderInitiated ? RefundReason.ProviderCancellation : RefundReason.UserCancellation;
        if (ev.ForceMajeureOverride) reason = RefundReason.ForceMajeureAdminOverride;

        var refund = payment.CreateRefund(refundAmount, reason, ev.AdminNotes, _timeProvider);
        await _paymentRepo.AddAsync(refund, ct);

        // Call gateway eagerly (don't wait for explicit /refund endpoint)
        try {
            var gw = await _gateway.RefundAsync(new RefundRequest(payment.GatewayTransactionId, refundAmount, payment.AmountTotal.Currency, reason.ToString()), ct);
            if (gw.Status == RefundStatus.Completed) refund.MarkRefundCompleted(gw.GatewayRefundId, _timeProvider.GetUtcNow().UtcDateTime);
            else if (gw.Status == RefundStatus.Failed) refund.MarkRefundFailed(gw.FailureCode ?? "Unknown");
        } catch (Exception ex) {
            _logger.LogError(ex, "Auto-refund gateway error");
            refund.MarkRefundFailed("GatewayError");
        }

        _inbox.MarkAsProcessed(e.EventId);
        await _uow.SaveChangesAsync(ct);
    }
}
```

---

## 5. GET endpoints — quick summary

- **GET /payments/{id}** — `{ id, bookingId, amount, currency, status, paymentMethod, refundedTotal, createdAt, completedAt? }`, includes nested `refunds: [...]` array.
- **GET /my-payments** — cursor-paginated, filter by `?status=Completed|Pending|Failed&fromDate=&toDate=`.
- **GET /admin/all** — same shape + `userId, providerId, currency` filters. Cache 30s.

All three implement `ICacheableQuery` per INDEX §4 R10.

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Domain methods (MarkCompleted/Failed/CreateRefund/MarkRefundCompleted/Failed) + tests | 5 | 2026-09-15 |
| 2 | Webhook signature verification (`VerifyWebhookSignatureAsync` impl) + IPaymentWebhookInbox dedupe | 4 | 2026-09-16 |
| 3 | Webhook handler dispatch + 4 sub-handlers (succeeded/failed × payment/refund) | 6 | 2026-09-19 |
| 4 | POST /refund endpoint + handler + validator | 5 | 2026-09-22 |
| 5 | `BookingTourBookingCancelledHandler` inbox handler + test | 3 | 2026-09-23 |
| 6 | 3 GET endpoints + queries + cache + tag builders | 4 | 2026-09-24 |
| 7 | Unit tests for webhook idempotency, refund amount validation, ownership | 3 | 2026-09-26 |
| 8 | Integration test: webhook → Booking inbox consumes → booking Confirmed | 1 | 2026-09-26 |
| 9 | PR review | 1 | 2026-09-27 |
| **Total** | | **32h** | |

---

## 7. Edge cases

1. Webhook arrives BEFORE Payment row written (race with T1's SaveChanges) → `GetByGatewayTransactionIdAsync` returns null, we log + 200; gateway will retry within seconds.
2. Webhook signature key rotated by Tech Lead → old webhooks pile up signature-mismatch; need 24h overlap with both secrets supported. (Add as PW-9b if needed; out of scope this sprint — log only.)
3. Partial refund + another partial refund → `RefundedTotal` accumulates; final refund hits exact `AmountTotal` → Status = RefundedFull.
4. Refund amount = full original → Status = RefundedFull immediately (no partial intermediate).
5. Gateway returns `Pending` status on refund → leave Payment row as Pending; T5 RefundRetryService picks up Failed ones, not Pending — Pending awaits webhook.
6. User cancels booking with 0% refund (last-minute) → inbox handler skips creating Refund row, logs info; no money movement.
7. Provider cancels booking → ALWAYS 100% (Booking computes this; inbox handler trusts the payload).
8. Same gateway event delivered twice → idempotency via `_webhookInbox.HasBeenProcessedAsync` on `cmd.EventId`. Both succeed with 200.
9. Webhook for refund where original payment is not Completed (impossible from gateway but defensive) → log error, 200 to stop retries.
10. Currency drift between booking snapshot and original Payment → reject refund with `Refund.CurrencyMismatch`. Means the booking snapshot is stale; T2 handler logs critical for ops to investigate.
