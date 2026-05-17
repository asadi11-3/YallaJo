# TASK 1 — POST /payments/initiate

> **Owner:** Mahmoud — **Hours:** 28h — **Hard deadline:** Sun **2026-09-13 17:00**
> **Earliest start:** Wed 2026-08-19 (after PW)
> **Endpoints:** 1 + 1 inbox handler
> **Depends on:** PW-3, PW-4, PW-5, PW-6 (gateway)

---

## 1. Endpoint Surface

| Method | Path | Permission | Returns |
|---|---|---|---|
| POST | `/api/v1/payments/initiate` | `Payment.Create` | 201 + `{ paymentId, gatewayPaymentId, redirectUrl?, clientSecret?, expiresAt }` |

**Inbox handler** (NOT an HTTP endpoint):
- `BookingTourBookingCreatedHandler` — consumes `booking.tour-booking.created.v1` → seeds a Payment expectation row (status `Awaiting`) tied to BookingId. This row gets PROMOTED to actual Payment when `POST /payments/initiate` is called. Avoids the order-of-operations problem of "booking created but no Payment yet exists to look up".

---

## 2. Request / Response

```http
POST /api/v1/payments/initiate
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "bookingId": "01956f60-...",
  "paymentMethod": "Card",                  // {Card, ApplePay, GooglePay, BankTransfer}
  "returnUrl": "https://yallajo.com/booking/confirm"
}
```

```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "paymentId": "01956f6a-...",
  "gatewayPaymentId": "gw-pay-01956f6a-...",
  "redirectUrl": "https://fake-gateway/checkout/abc",   // when redirect-based gateway
  "clientSecret": null,                                 // when SDK-based gateway (Stripe pattern)
  "expiresAt": "2026-09-01T10:15:00Z"
}
```

---

## 3. Validation (FluentValidation)

- `bookingId` not empty Guid
- `paymentMethod` ∈ enum
- `returnUrl` valid HTTPS URL within configured allow-list (`Finance:Gateway:ReturnUrlAllowedHosts`)
- `Authorization` Bearer valid (handled by JWT middleware)

---

## 4. Handler Flow

```csharp
public async Task<Result<InitiatePaymentResponse>> Handle(InitiatePaymentCommand cmd, CancellationToken ct)
{
    // 1. Look up booking via snapshot (we DON'T cross-call Booking module — read snapshot from inbox)
    var bookingExpectation = await _paymentRepo.GetExpectationByBookingIdAsync(cmd.BookingId, ct);
    if (bookingExpectation is null)
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.BookingNotEligible", "No booking expectation found"), Outcome.Validation);

    // 2. Self-ownership check
    if (bookingExpectation.UserId != _currentUser.UserId)
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.OwnerMismatch", "Not your booking"), Outcome.Forbidden);

    // 3. Booking must be in AwaitingPayment state (per snapshot)
    if (bookingExpectation.Status != "AwaitingPayment")
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.BookingNotEligible", "Booking not in AwaitingPayment"), Outcome.Conflict);

    // 4. Idempotency: if a Payment already exists for this booking that's not Failed, reuse it
    var existing = await _paymentRepo.GetActiveByBookingIdAsync(cmd.BookingId, ct);
    if (existing is not null && existing.Status is PaymentStatus.Pending)
        return Result.Success(MapToResponse(existing)); // 200 with existing details

    // 5. Create Payment aggregate via factory
    var payment = Payment.Initiate(
        bookingId: cmd.BookingId,
        userId: bookingExpectation.UserId,
        providerId: bookingExpectation.ProviderId,
        amount: new Money(bookingExpectation.AmountTotal, bookingExpectation.Currency),
        paymentMethod: cmd.PaymentMethod,
        gatewayProvider: _gateway.GatewayName,
        timeProvider: _timeProvider);
    // Raises PaymentInitiatedDomainEvent

    // 6. Call gateway. Wrap in Polly retry (Polly v8 — INDEX §4 R12 lets us catch in Infrastructure).
    var initRequest = new InitiateRequest(
        PaymentId: payment.Id,
        Amount: payment.AmountTotal.Amount,
        Currency: payment.AmountTotal.Currency,
        DescribedAs: $"YallaJo booking {payment.BookingId}",
        ReturnUrl: new Uri(cmd.ReturnUrl),
        Metadata: new Dictionary<string,string> { ["BookingId"] = payment.BookingId.ToString(), ["UserId"] = payment.UserId.ToString() });

    InitiateResult gw;
    try { gw = await _gateway.InitiateAsync(initRequest, ct); }
    catch (Exception ex)
    {
        // Polly already retried; this is final
        _logger.LogError(ex, "Gateway initiate failed for payment {PaymentId}", payment.Id);
        return Result.Failure<InitiatePaymentResponse>(new Error("Payment.GatewayError", "Payment gateway unavailable"), Outcome.ExternalServiceError);
    }

    payment.StampGatewayInitiation(gw.GatewayPaymentId, gw.RedirectUrl?.ToString(), gw.ClientSecret);

    // 7. Persist + emit outbox + invalidate cache
    await _paymentRepo.AddAsync(payment, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    await _cache.RemoveByTagAsync($"payments:booking:{payment.BookingId}", ct);

    return Result.Success(MapToResponse(payment));
}
```

---

## 5. Domain methods on `Payment` (T1 deliverables)

```csharp
public static Payment Initiate(Guid bookingId, Guid userId, Guid providerId, Money amount, PaymentMethod paymentMethod, string gatewayProvider, TimeProvider timeProvider);
public Result StampGatewayInitiation(string gatewayPaymentId, string? redirectUrl, string? clientSecret);
public Result MarkExpired(DateTime nowUtc);  // for T2 webhook timeout case
```

Factory `Initiate` raises `PaymentInitiatedDomainEvent`.

---

## 6. Cache & Tags

| Cache | Key | TTL | Tag |
|---|---|---|---|
| Payment-by-id | `payment:{paymentId}` | 10 min | `payment:{paymentId}`, `payments:user:{userId}` |
| Payment-by-booking lookup | `payments:booking:{bookingId}` | 1 min | `payments:booking:{bookingId}`, `payments:user:{userId}` |

Initiate command invalidates `payments:booking:{bookingId}` after save.

---

## 7. Inbox Handler — `BookingTourBookingCreatedHandler`

```csharp
internal sealed class BookingTourBookingCreatedHandler(
    IFinanceInboxStore inbox,
    IPaymentExpectationRepository expectationRepo,
    IFinanceUnitOfWork uow,
    ILogger<BookingTourBookingCreatedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventEnvelope<BookingTourBookingCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventEnvelope<BookingTourBookingCreatedIntegrationEvent> e, CancellationToken ct)
    {
        if (await inbox.HasBeenProcessedAsync(e.EventId, ct)) return;

        var ev = e.Payload;
        var expectation = PaymentExpectation.Create(
            bookingId: ev.BookingId,
            userId: ev.UserId,
            providerId: ev.ProviderId,
            amount: new Money(ev.TotalAmount, ev.Currency),
            status: "AwaitingPayment",
            createdAt: timeProvider.GetUtcNow().UtcDateTime);

        await expectationRepo.AddAsync(expectation, ct);
        inbox.MarkAsProcessed(e.EventId);
        await uow.SaveChangesAsync(ct);  // single SaveChanges
    }
}
```

`PaymentExpectation` is a lightweight read-snapshot entity (BaseEntity, no events). Lives in `Finance.Infrastructure/Persistence/Snapshots/`. Owned by Finance, no migration script needed beyond the table.

---

## 8. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `Payment.Initiate` factory + `StampGatewayInitiation` + `MarkExpired` + tests | 4 | 2026-08-21 |
| 2 | `PaymentExpectation` snapshot entity + repo + config + migration | 3 | 2026-08-24 |
| 3 | `BookingTourBookingCreatedHandler` inbox handler + test | 3 | 2026-08-26 |
| 4 | `InitiatePaymentCommand` + handler + validator | 5 | 2026-08-31 |
| 5 | Gateway DI registration + `FakePaymentGateway` impl + Polly retry policy | 4 | 2026-09-02 |
| 6 | Endpoint wiring + permission attribute + Swagger XML doc | 2 | 2026-09-03 |
| 7 | Cache builders + tag invalidation | 1 | 2026-09-03 |
| 8 | Unit tests (10+) covering validators, handler branches, factory invariants | 3 | 2026-09-08 |
| 9 | Integration test: booking created → expectation row → POST initiate succeeds + outbox row | 2 | 2026-09-10 |
| 10 | PR review fixes | 1 | 2026-09-13 |
| **Total** | | **28h** | |

---

## 9. Edge cases

1. Repeat POST initiate for same booking when active Payment exists → return existing details, do NOT create duplicate.
2. Booking auto-expires (Booking BG service) while initiate in flight → gateway call succeeds, but our state will be reconciled on `booking.tour-booking.payment-expired.v1` inbox consumer (T2) which cancels the gateway side and marks Payment Failed.
3. `PaymentExpectation` not yet seeded (booking event arrived after initiate due to inbox lag) → 422 with `Payment.BookingNotEligible`, user retries.
4. Currency mismatch (booking snapshot in JOD, user override URL claims USD) → reject — we always use the snapshot currency, never trust request body for amount/currency.
5. Network partition on gateway call → Polly retries 3× with exponential backoff; final failure returns 502.
6. Gateway returns 200 but no `GatewayPaymentId` → treat as failure, log + return 502.
