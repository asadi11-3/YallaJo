# Workflow 06 — Booking Lifecycle

The **core monetization flow**. From browsing availability to a completed paid booking, including cancellations, timeouts, and provider-confirmed paths.

---

## At a glance

| | |
|---|---|
| **Trigger** | `POST /booking/tour` (authenticated user) |
| **Owner module** | Booking |
| **Cross-module reach** | Finance (payment, invoice, payout), Messaging (notifications), Analytics (snapshots) |
| **Key entities** | `TourBooking`, `SlotLock`, `AvailabilitySlot`, `Reservation`, `PackageBooking`, `RefundPolicy`, `BookingPricing` |
| **Key enums** | `BookingStatus`, `ConfirmationSource`, `CancellationSource`, `SlotType` |

---

## Actors

```mermaid
flowchart LR
    Tourist([Tourist])
    Provider([Provider])
    Admin([Admin])
    Gateway[[Payment Gateway<br/>(stub)]]
    BG[[Background services]]

    Tourist -->|create / cancel| API
    Provider -->|confirm / reject / complete| API
    Admin -->|force refund| API
    Gateway -->|webhook| API
    BG -->|auto-expire / auto-accept / slot cleanup| API
    API[(YallaJo.Api)]
```

---

## `BookingStatus` state machine

```mermaid
stateDiagram-v2
    [*] --> AwaitingPayment: CreateTourBooking (+SlotLock)
    AwaitingPayment --> PendingConfirmation: PaymentCompleted (non-instant tour)
    AwaitingPayment --> Confirmed: PaymentCompleted (instant tour)
    AwaitingPayment --> Cancelled: User CancelTourBooking
    AwaitingPayment --> Cancelled: BookingAutoExpireService (TTL elapsed)
    AwaitingPayment --> Cancelled: SlotLock expired (PaymentExpired)
    PendingConfirmation --> Confirmed: Provider ConfirmTourBooking
    PendingConfirmation --> Confirmed: ProviderAutoAcceptService (>24h)
    PendingConfirmation --> Rejected: Provider RejectTourBooking
    Confirmed --> Completed: Provider CompleteTourBooking
    Confirmed --> Cancelled: User / Admin cancellation
    Rejected --> [*]
    Cancelled --> [*]
    Completed --> [*]
```

> Source enums: `Booking.Domain/Enums/BookingStatus.cs`, `ConfirmationSource.cs`, `CancellationSource.cs`.

---

## Happy-path sequence (instant booking)

```mermaid
sequenceDiagram
    autonumber
    actor U as Tourist
    participant API as Booking.Presentation
    participant BApp as Booking.Application
    participant BDB as booking DB
    participant BOB as Booking.Outbox
    participant CO as CompositeOutboxProcessor
    participant FIB as Finance.Inbox
    participant FApp as Finance.Application
    participant Gateway as Payment Gateway
    participant FOB as Finance.Outbox
    participant BIB as Booking.Inbox
    participant MIB as Messaging.Inbox
    participant Msg as Messaging

    U->>API: POST /booking/tour {tourId, slotId, participants}
    API->>BApp: CreateTourBookingCommand
    BApp->>BDB: Create TourBooking (AwaitingPayment) + SlotLock (TTL ~10m)
    BApp->>BOB: TourBookingCreatedIntegrationEvent + SlotLockCreated
    BApp-->>U: 200 OK {bookingId, reference}

    CO->>FIB: deliver TourBookingCreated
    FIB->>FApp: create PaymentExpectation

    U->>API: POST /finance/payments/initiate {bookingId}
    API->>FApp: InitiatePaymentCommand
    FApp->>Gateway: Charge() (stub returns URL/token)
    FApp-->>U: 200 OK {paymentUrl}

    Gateway-->>API: POST /finance/payments/webhook {paymentId, status=Succeeded}
    API->>FApp: ProcessWebhookCommand
    FApp->>FOB: PaymentCompletedIntegrationEvent + InvoiceGeneratedIntegrationEvent
    CO->>BIB: deliver PaymentCompleted
    BIB->>BApp: TourBooking → Confirmed (instant tour), release SlotLock, decrement capacity
    BApp->>BOB: TourBookingConfirmedIntegrationEvent
    CO->>MIB: deliver TourBookingConfirmed
    Msg-->>U: Notification (in-app + email)
    Msg-->>U: Notification (provider notified too)
```

---

## Provider-confirmed path (non-instant)

```mermaid
sequenceDiagram
    autonumber
    actor P as Provider
    participant API as Booking.Presentation
    participant BApp as Booking.Application
    participant BOB as Booking.Outbox
    participant CO as CompositeOutboxProcessor
    participant MIB as Messaging.Inbox

    Note over BApp: PaymentCompleted earlier → status = PendingConfirmation
    P->>API: POST /booking/{id}/confirm
    API->>BApp: ConfirmTourBookingCommand (provider auth + ownership)
    BApp->>BApp: TourBooking → Confirmed (source = Provider)
    BApp->>BOB: TourBookingConfirmedIntegrationEvent
    CO->>MIB: deliver → notify tourist + provider

    Note over BApp: If provider doesn't confirm in 24h,<br/>ProviderAutoAcceptService confirms with source = Auto
```

---

## Cancellation & refund

```mermaid
sequenceDiagram
    autonumber
    actor U as Tourist
    participant API as Booking.Presentation
    participant BApp as Booking.Application
    participant BOB as Booking.Outbox
    participant CO as CompositeOutboxProcessor
    participant FIB as Finance.Inbox
    participant FApp as Finance.Application
    participant Gateway as Payment Gateway

    U->>API: POST /booking/{id}/cancel
    API->>BApp: CancelTourBookingCommand
    BApp->>BApp: Evaluate RefundPolicy → compute refundAmount
    BApp->>BApp: TourBooking → Cancelled, restore capacity
    BApp->>BOB: TourBookingCancelledIntegrationEvent (+ refundAmount)
    CO->>FIB: deliver TourBookingCancelled
    FIB->>FApp: RefundPayment if refundAmount > 0
    FApp->>Gateway: Refund() (stub)
    FApp-->>FApp: emit RefundInitiated → RefundCompleted/Failed
    Note over FApp: RefundRetryService retries failed refunds
```

---

## Timeout & background paths

| Background service | Schedule | Effect |
|---|---|---|
| `SlotLockCleanupService` | every ~5 min | Deletes expired `SlotLock` rows; releases capacity |
| `BookingAutoExpireService` | every ~5 min | Cancels `AwaitingPayment` bookings whose TTL elapsed → emits `TourBookingPaymentExpired` |
| `ProviderAutoAcceptService` | every ~15 min | Confirms `PendingConfirmation` bookings older than the configured threshold (~24h) |
| `DocumentExpiryCheckService` | daily | Marks `ProviderDocument` expired/expiring → can suspend provider |

Files in `Booking.Infrastructure/BackgroundServices/`.

---

## Side effects (events)

| Event | Producer | Notable consumers |
|---|---|---|
| `TourBookingCreatedIntegrationEvent` | Booking | Finance (`PaymentExpectation`), Messaging, Analytics |
| `TourBookingConfirmedIntegrationEvent` | Booking | Messaging (notify both parties), Analytics |
| `TourBookingCancelledIntegrationEvent` | Booking | Finance (refund), Messaging, Analytics |
| `TourBookingRejectedIntegrationEvent` | Booking | Finance (refund), Messaging |
| `TourBookingCompletedIntegrationEvent` | Booking | Finance (payout eligibility), Analytics, Social (enables review) |
| `TourBookingPaymentExpiredIntegrationEvent` | Booking | Finance (mark expectation expired), Messaging |
| `SlotLockCreated/Released` | Booking | (internal capacity bookkeeping) |
| `AvailabilitySlotCapacityChanged` | Booking | Cache invalidation |
| `JoinRequestCreated/Approved/Rejected` | Booking | Messaging |
| `PaymentCompletedIntegrationEvent` | Finance | Booking (confirm), Messaging |
| `PaymentFailedIntegrationEvent` | Finance | Booking (stay AwaitingPayment), Messaging |
| `RefundInitiated/Completed/Failed` | Finance | Messaging |

---

## Authorization

| Action | Required |
|---|---|
| Create booking | Authenticated user |
| Cancel own booking | Authenticated user, ownership check |
| Confirm / reject / complete | Provider role + `ITourGuideOwnershipService` / tour ownership |
| Admin force refund | `Admin` policy |
| List all bookings | `Admin` policy |

Permission catalog: `Booking.Contracts/Authorization/BookingPermissionCatalog.cs`.

---

## Code references

- `Booking.Application/Commands/CreateTourBooking/`
- `Booking.Application/Commands/CancelTourBooking/`
- `Booking.Application/Commands/ConfirmTourBooking/`
- `Booking.Application/Commands/RejectTourBooking/`
- `Booking.Application/Commands/CompleteTourBooking/`
- `Booking.Application/Commands/AdminForceRefund/`
- `Booking.Domain/Entities/TourBooking.cs`, `SlotLock.cs`, `AvailabilitySlot.cs`, `RefundPolicy.cs`
- `Booking.Infrastructure/BackgroundServices/{SlotLockCleanupService,BookingAutoExpireService,ProviderAutoAcceptService,DocumentExpiryCheckService}.cs`
- `Booking.Infrastructure/EventHandlers/BookingIntegrationConverters.cs`
- `Booking.Infrastructure/EventHandlers/SlotCapacityRestoreHandlers.cs`
- `Booking.Infrastructure/EventHandlers/TourBookingIntegrationConverters.cs`
- `Finance.Application/EventHandlers/BookingTourBookingCreatedHandler.cs`
- `Finance.Application/EventHandlers/BookingTourBookingCancelledHandler.cs`
- `Finance.Application/EventHandlers/BookingTourBookingCompletedHandler.cs`
- `Finance.Application/Commands/ProcessWebhook/`
- `Messaging.Infrastructure/EventHandlers/Booking*Handler.cs`

---

## Related risks

- [`RISK-002`](../risks/risk-register.md) — `FakePaymentGateway` is a stub; webhook signature verification is not enforced.
- [`RISK-003`](../risks/risk-register.md) — Booking depends on `Stub*SnapshotReader` services for pricing/provider/tour snapshots.
- [`RISK-007`](../risks/risk-register.md) — Background services use `PeriodicTimer` with no distributed lock; scale-out risk.
- [`RISK-008`](../risks/risk-register.md) — `NoOpDiscountEvaluator` ⇒ discounts are not applied.
- [`RISK-011`](../risks/risk-register.md) — Outbox dead-letter exists; reliability hardening still in progress.

Cross-reference: [`../Agents/tasks/Booking/`](../../Agents/tasks/Booking/) for sprint-level task breakdown.
