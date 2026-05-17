# TASK 7 — Booking Background Services

> **Owner:** Mahmoud (Intermediate) — **Hours:** 24h (10h base + 14h tests/hardening) — **Hard deadline:** Sun **2026-08-09 17:00**
> **Earliest start:** Wed 2026-07-15 (after TASK 5 cancel/expire domain methods land — services consume them)
> **Endpoints:** 0 HTTP. **4 BackgroundService implementations.**
> **Depends on:** PW-1 (UoW dispatch), PW-3 (domain events with payload fields), TASK 1 (slot capacity restore), TASK 5 (booking expire/cancel state machine), TASK 3 (ProviderDocument.MarkExpiringSoon/Expired)

This task wires the four cron-style hosted services that drive Booking's time-based invariants. **No Hangfire / Quartz — pure `BackgroundService` + `PeriodicTimer` per ADR-003 (agent-context.md §0.2).**

---

## 0. Service Catalog

| # | Service | Cadence | Project | Trigger event(s) emitted |
|---|---|---|---|---|
| 1 | `SlotLockCleanupService` | every **5 min** | Booking.Infrastructure | `SlotLockReleasedDomainEvent` per expired lock → integration `booking.slot-lock.expired.v1` |
| 2 | `BookingAutoExpireService` | every **5 min** | Booking.Infrastructure | `TourBookingPaymentExpiredDomainEvent` per expired booking → integration `booking.tour-booking.payment-expired.v1` |
| 3 | `ProviderAutoAcceptService` | every **15 min** | Booking.Infrastructure | `TourBookingConfirmedDomainEvent` with `ConfirmationSource.Auto` → integration `booking.tour-booking.confirmed.v1` |
| 4 | `DocumentExpiryCheckService` | **daily 01:00 UTC** | Booking.Infrastructure | `ProviderDocumentExpiringDomainEvent` (30-day warning) / `ProviderDocumentExpiredDomainEvent` / `ProviderSuspendedDocumentExpiredDomainEvent` |

All four registered in `Booking.Infrastructure/DependencyInjection.cs` via `services.AddHostedService<TService>()`. **Order matters for shutdown** — register LIFO inverse of dependency: lock cleanup last (least dependent), document check first.

---

## 1. Shared Pattern (all 4 services follow this)

```csharp
internal sealed class XxxService(
    IServiceProvider serviceProvider,           // NOT IServiceScopeFactory — use BuildServiceProvider().CreateScope() pattern
    ILogger<XxxService> logger,
    TimeProvider timeProvider,                  // YallaJo SharedKernel-registered
    IOptions<XxxServiceOptions> options)
    : BackgroundService
{
    private readonly TimeSpan _interval = options.Value.Interval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        logger.LogInformation("{Service} started; interval = {Interval}", nameof(XxxService), _interval);
        try
        {
            // Optional initial offset so multiple services don't fire on same tick:
            await Task.Delay(options.Value.InitialDelay, stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var activity = BookingDiagnostics.ActivitySource.StartActivity(nameof(XxxService));
                using var scope = serviceProvider.CreateScope();
                try
                {
                    await ProcessBatchAsync(scope.ServiceProvider, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Service} tick failed; will retry next interval", nameof(XxxService));
                    BookingDiagnostics.BgServiceFailures.Add(1, new("service", nameof(XxxService)));
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { /* graceful */ }
        finally
        {
            logger.LogInformation("{Service} stopped", nameof(XxxService));
        }
    }

    private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct) { /* per-service */ }
}
```

**Try/catch policy** (INDEX §4 R12): only **Infrastructure** BG service loops may catch general `Exception`. The wrapper catch above MUST re-throw `OperationCanceledException when ct.IsCancellationRequested` and MUST log + swallow everything else (so a transient SQL deadlock doesn't kill the service).

**OpenTelemetry**: `BookingDiagnostics.ActivitySource = new("YallaJo.Booking")` already created in PW; meter counters `bg_service_ticks_total`, `bg_service_failures_total`, `bg_service_items_processed_total` tagged with `service`.

**Service options pattern**: each service has `XxxServiceOptions { Interval, InitialDelay, BatchSize, Enabled }` bound from `Booking:BackgroundServices:Xxx` config section. `Enabled=false` lets ops disable a service without redeploy.

---

## 2. SlotLockCleanupService (5 min)

**Goal:** flip `SlotLock.IsActive=false` on rows where `ExpiresAt < now`, restore capacity, raise events.

```csharp
private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
{
    var unitOfWork = scoped.GetRequiredService<IBookingUnitOfWork>();
    var lockRepo   = scoped.GetRequiredService<ISlotLockRepository>();
    var now        = timeProvider.GetUtcNow().UtcDateTime;

    var expired = await lockRepo.GetExpiredAsync(now, options.Value.BatchSize, ct);
    if (expired.Count == 0) { logger.LogDebug("No expired slot locks"); return; }

    foreach (var slotLock in expired)
    {
        // SlotLock.MarkReleased raises SlotLockReleasedDomainEvent (PW-3 record)
        slotLock.MarkReleased(now);
    }

    // UoW dispatches SlotLockReleasedDomainEvent → handler (TASK 1 sibling) calls AvailabilitySlot.Unlock → raises AvailabilitySlotCapacityChangedDomainEvent.
    // Integration event booking.slot-lock.expired.v1 emitted via outbox (PW-4).
    await unitOfWork.SaveChangesAsync(ct);
    BookingDiagnostics.BgServiceItemsProcessed.Add(expired.Count, new("service", nameof(SlotLockCleanupService)));
}
```

**BatchSize:** default **500** (config). Larger batches risk long-running SaveChanges. If `expired.Count == BatchSize`, log warning so ops can investigate lock-accumulation.

**Idempotency:** `MarkReleased` guards `if (!IsActive) return Result.Success();` — re-running is safe.

**Edge cases:**
1. SaveChanges throws DbUpdateConcurrencyException because another tick already released same lock → catch in outer wrapper, log info, retry next tick.
2. AvailabilitySlot doesn't exist anymore (tour deleted while lock held) → handler logs warning and skips capacity restore — lock still released.
3. Clock skew (server clock > DB clock by minutes) → `now` driven by `TimeProvider`, no issue.

---

## 3. BookingAutoExpireService (5 min)

**Goal:** cancel `TourBooking` rows stuck in `AwaitingPayment` past 10-minute payment window (PDF 2 §1.4 — "auto-cancel if payment not within 10min").

```csharp
private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
{
    var bookingRepo = scoped.GetRequiredService<ITourBookingRepository>();
    var uow         = scoped.GetRequiredService<IBookingUnitOfWork>();
    var cutoff      = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-options.Value.PaymentWindowMinutes); // default 10

    var stuck = await bookingRepo.GetAwaitingPaymentOlderThanAsync(cutoff, options.Value.BatchSize, ct);
    foreach (var booking in stuck)
    {
        // MoveToAwaitingPaymentExpired raises TourBookingPaymentExpiredDomainEvent (PW-3)
        // Handler in Booking.Infrastructure/EventHandlers/ restores AvailabilitySlot capacity.
        // Integration event booking.tour-booking.payment-expired.v1 emitted via outbox.
        var result = booking.MoveToAwaitingPaymentExpired(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) logger.LogWarning("Auto-expire failed for booking {Id}: {Error}", booking.Id, result.Error?.Code);
    }
    await uow.SaveChangesAsync(ct);
}
```

**Why we don't auto-cancel by `SlotLock.ExpiresAt`:** the lock can release seconds before the booking expires in race conditions. Source of truth is the booking row, not the lock. We sweep both independently.

**`PaymentWindowMinutes` config knob**: 10 in prod, 1 in dev/test fixtures so integration tests don't wait.

**Notification side-effect:** the integration event `booking.tour-booking.payment-expired.v1` is consumed by Messaging inbox → notification to user "Your booking expired — please try again." Messaging sprint owns the template.

**Edge cases:**
1. Booking already Cancelled (race with user manual cancel) → state guard on `MoveToAwaitingPaymentExpired` returns `Error('TourBooking.InvalidState', ...)`; we log debug and move on.
2. Booking already Confirmed (race with webhook arriving 10:00.001 after cutoff) → same state guard.
3. Provider deleted between booking creation and expiry → still cancel; downstream events stay valid.

---

## 4. ProviderAutoAcceptService (15 min)

**Goal:** auto-confirm `TourBooking` rows in `PendingConfirmation` past 24h (PDF 2 §1.4 — "auto-confirm after 24h if no response").

```csharp
private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
{
    var bookingRepo = scoped.GetRequiredService<ITourBookingRepository>();
    var uow         = scoped.GetRequiredService<IBookingUnitOfWork>();
    var cutoff      = timeProvider.GetUtcNow().UtcDateTime.AddHours(-options.Value.ProviderConfirmationHours); // default 24

    var stuck = await bookingRepo.GetPendingConfirmationOlderThanAsync(cutoff, options.Value.BatchSize, ct);
    foreach (var booking in stuck)
    {
        // Confirm(ConfirmationSource.Auto) idempotent — raises TourBookingConfirmedDomainEvent exactly once (B-R6 in 02-critical-rules.md)
        var result = booking.Confirm(ConfirmationSource.Auto, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) logger.LogWarning("Auto-confirm failed for booking {Id}: {Error}", booking.Id, result.Error?.Code);
    }
    await uow.SaveChangesAsync(ct);
}
```

**Why 15 min not 5 min:** auto-confirm windows are days-long; lower cadence cuts SQL pressure.

**Notification:** integration event `booking.tour-booking.confirmed.v1` (with `Source=Auto` discriminator) → Messaging sends "Your booking is confirmed" + optionally a "Your tour was auto-confirmed because the provider didn't respond in time" admin alert.

**Edge cases:**
1. Provider responds in same 15-min window → user manual Confirm wins (state guard returns InvalidState here, we log + skip).
2. Booking was Rejected by provider after cutoff but before sweep → state guard, skip.

---

## 5. DocumentExpiryCheckService (daily 01:00 UTC)

**Goal:** warn providers 30 days before document expiry; mark expired and trigger provider suspension for CRITICAL doc types (06-task-provider-documents.md §B-R9).

```csharp
private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
{
    var docRepo = scoped.GetRequiredService<IProviderDocumentRepository>();
    var uow     = scoped.GetRequiredService<IBookingUnitOfWork>();
    var now     = timeProvider.GetUtcNow().UtcDateTime;

    // 1. 30-day warnings — Approved, not yet warned, expires within 30 days
    var expiring = await docRepo.GetExpiringWithinAsync(daysAhead: 30, batchSize: options.Value.BatchSize, ct);
    foreach (var doc in expiring)
    {
        var daysLeft = (int)Math.Ceiling((doc.ExpiresAt!.Value - now).TotalDays);
        // MarkExpiringSoon raises ProviderDocumentExpiringDomainEvent (PW-3); state guard prevents re-warning.
        doc.MarkExpiringSoon(daysLeft);
    }

    // 2. Hard expiry — Approved, expired now, not yet processed
    var expired = await docRepo.GetExpiredAsync(now, options.Value.BatchSize, ct);
    foreach (var doc in expired)
    {
        // MarkExpired raises ProviderDocumentExpiredDomainEvent.
        // If doc.Type is CRITICAL, the entity additionally raises ProviderSuspendedDocumentExpiredDomainEvent
        // → handler emits integration booking.provider.suspended-doc-expired.v1 → Accounts inbox suspends provider.
        doc.MarkExpired(now);
    }

    await uow.SaveChangesAsync(ct);
    logger.LogInformation("DocumentExpiry tick: {Warnings} warnings, {Expirations} expirations", expiring.Count, expired.Count);
}
```

**Cron at 01:00 UTC** — not `PeriodicTimer(24h)` because that drifts. Implementation: compute next 01:00 UTC and `Task.Delay` to it, then loop with `PeriodicTimer(24h)` thereafter. Helper `NextOccurrenceUtc(TimeOnly target)` in `SharedKernel.Infrastructure/Time/`.

**Why not run hourly:** doc expiry is date-granular; running 24× per day wastes IO. Acceptable SLA = 24h delay.

**`ExpiryWarningSent` and `ExpiryProcessed` flags** (06-task-provider-documents.md migration) prevent duplicate warnings and double-suspension. Daily run is fully idempotent.

**CRITICAL document types per PDF 2 §1.1**: `MoTALicense, InsuranceCertificate, LiabilityInsurance, HealthSafetyCertificate, FireSafetyCertificate, TourismAuthorityLicense` — non-critical (e.g. PersonalId, FirstAidCert) expire but only warn, do NOT auto-suspend (provider can renew without losing platform access).

**Confirmed bookings stay valid** on suspension per B-R9 — only new bookings are blocked (POST /tour checks BookingProviderSnapshot.Status == Active). PDF 2 §1.1 "auto-suspend if not renewed within 14 days" — 14-day grace already baked into the 30-day warning window (provider gets 30 days, then suspension; they have 14 days during suspension to renew before listings permanently hidden — Accounts module owns the 14-day permanent-hide policy).

**Edge cases:**
1. Same doc warned, then expires same week → second tick raises Expired event; idempotent thanks to flags.
2. Provider replaces doc with new ExpiresAt → 06-task says approving a new doc clears prior — handled in POST /provider/documents flow, not here.
3. Service down for 3 days → batch picks up backlog on restart (no time-bucketing logic to lose).

---

## 6. DI Registration (Booking.Infrastructure/DependencyInjection.cs)

```csharp
public static IServiceCollection AddBookingInfrastructure(this IServiceCollection services, IConfiguration cfg)
{
    // ... DbContext, repositories, UoW, inbox, outbox ...

    services.Configure<SlotLockCleanupServiceOptions>(cfg.GetSection("Booking:BackgroundServices:SlotLockCleanup"));
    services.Configure<BookingAutoExpireServiceOptions>(cfg.GetSection("Booking:BackgroundServices:BookingAutoExpire"));
    services.Configure<ProviderAutoAcceptServiceOptions>(cfg.GetSection("Booking:BackgroundServices:ProviderAutoAccept"));
    services.Configure<DocumentExpiryCheckServiceOptions>(cfg.GetSection("Booking:BackgroundServices:DocumentExpiryCheck"));

    services.AddHostedService<DocumentExpiryCheckService>();   // LIFO: stops first on shutdown
    services.AddHostedService<ProviderAutoAcceptService>();
    services.AddHostedService<BookingAutoExpireService>();
    services.AddHostedService<SlotLockCleanupService>();       // LIFO: stops last
    return services;
}
```

**`appsettings.json` defaults:**
```json
"Booking": {
  "BackgroundServices": {
    "SlotLockCleanup":     { "Interval": "00:05:00", "InitialDelay": "00:00:30", "BatchSize": 500, "Enabled": true },
    "BookingAutoExpire":   { "Interval": "00:05:00", "InitialDelay": "00:01:00", "BatchSize": 500, "PaymentWindowMinutes": 10, "Enabled": true },
    "ProviderAutoAccept":  { "Interval": "00:15:00", "InitialDelay": "00:02:00", "BatchSize": 500, "ProviderConfirmationHours": 24, "Enabled": true },
    "DocumentExpiryCheck": { "TargetUtcTime": "01:00:00", "BatchSize": 1000, "Enabled": true }
  }
}
```

**`appsettings.Development.json` overrides** for fast feedback:
- `SlotLockCleanup.Interval = 00:00:30`
- `BookingAutoExpire.PaymentWindowMinutes = 1`
- `ProviderAutoAccept.ProviderConfirmationHours = 0.1` (6 min)

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Options classes + appsettings defaults + dev overrides | 2 | 2026-07-16 |
| 2 | `SlotLockCleanupService` impl + unit test | 4 | 2026-07-21 |
| 3 | `BookingAutoExpireService` impl + unit test | 4 | 2026-07-23 |
| 4 | `ProviderAutoAcceptService` impl + unit test | 3 | 2026-07-28 |
| 5 | `DocumentExpiryCheckService` impl + unit test + `NextOccurrenceUtc` helper | 6 | 2026-07-30 |
| 6 | `BookingDiagnostics` static class (ActivitySource + Meter + counters) | 2 | 2026-07-30 |
| 7 | DI registration + LIFO order + boot log smoke | 1 | 2026-07-31 |
| 8 | Integration test: SlotLock TTL expires → AvailabilitySlot capacity restored within 15 min worst-case | 2 | 2026-08-04 |
| 9 | Integration test: AwaitingPayment > 10 min → Cancelled + outbox row | 1 | 2026-08-05 |
| 10 | PR review fixes | 1 | 2026-08-09 |
| **Total** | | **24h** | **Sun 2026-08-09** |

---

## 8. Acceptance gate for TASK 7

1. **Boot log shows all 4 services started** with their interval. Disabling any via config skips registration silently with `LogInformation("{Service} disabled via config, skipping registration")`.
2. **`/health/live` includes BG services** — register `services.AddHealthChecks().AddCheck<BookingBgServicesHealthCheck>("booking-bg")` that returns Unhealthy if any of the 4 services hasn't ticked within 3× its interval.
3. **OpenTelemetry counters** visible in dev OTLP exporter: `bg_service_ticks_total{service=SlotLockCleanupService}` increments every 5 min.
4. **Manual integration test (Postman + sleep + DB query)**:
   - Create AvailabilitySlot capacity=5, create SlotLock count=2 with ExpiresAt=now-1min, wait 5+ min → assert `AvailabilitySlot.LockedCount=0`, `SlotLock.IsActive=false`, outbox row `booking.slot-lock.expired.v1` present.
   - Create TourBooking AwaitingPayment with CreatedAt=now-11min, wait 5+ min → assert Status=Cancelled, outbox row `booking.tour-booking.payment-expired.v1`, slot capacity restored.
   - Create TourBooking PendingConfirmation with TransitionedToPendingAt=now-25h, wait 15+ min → assert Status=Confirmed, ConfirmationSource=Auto, outbox row `booking.tour-booking.confirmed.v1`.
   - Create ProviderDocument MoTALicense ExpiresAt=now-1day, trigger service manually via admin endpoint (added in 11-cross-cutting.md as bonus): assert Status=Expired, ExpiryProcessed=true, outbox row `booking.provider.suspended-doc-expired.v1`.
5. **No CS warnings** in build output. **No uncaught exceptions** in 24h soak test (run in pre-prod environment).
