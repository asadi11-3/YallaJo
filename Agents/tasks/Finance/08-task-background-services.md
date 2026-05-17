# TASK 5 — Background Services (PayoutBatching + RefundRetry)

> **Owner:** Mohammad — **Hours:** 28h — **Hard deadline:** Sun **2026-10-11 17:00**
> **Earliest start:** Mon 2026-10-05 (after T4)
> **Endpoints:** 0 HTTP. **2 BackgroundService implementations** + (optional bonus) admin manual-trigger endpoints (already done via T4's POST /admin/trigger for batching).
> **Depends on:** T4 (payout state machine), T2 (refund creation flow), Booking 10-task pattern reference

Follows the shared pattern documented in `../Booking/10-task-background-services.md §1`. Uses `BackgroundService` + `PeriodicTimer` + `IServiceProvider.CreateScope` per ADR-003.

---

## 1. PayoutBatchingService (weekly Sun midnight UTC)

**Goal:** automate `POST /payouts/admin/trigger` flow on a cron schedule.

```csharp
internal sealed class PayoutBatchingService(
    IServiceProvider sp,
    ILogger<PayoutBatchingService> logger,
    TimeProvider tp,
    IOptions<PayoutBatchingOptions> opts)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opts.Value.Enabled) { logger.LogInformation("PayoutBatchingService disabled"); return; }
        logger.LogInformation("PayoutBatchingService started, scheduling for Sunday midnight UTC");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = NextWeekly(tp.GetUtcNow().UtcDateTime, opts.Value.TargetDayOfWeek, opts.Value.TargetTimeUtc);
            var wait = nextRun - tp.GetUtcNow().UtcDateTime;
            logger.LogInformation("Next payout batch at {Next:o} (in {Hours:0.0}h)", nextRun, wait.TotalHours);
            try { await Task.Delay(wait, stoppingToken); }
            catch (OperationCanceledException) { return; }

            using var scope = sp.CreateScope();
            try
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new TriggerPayoutCommand(IsManual: false), stoppingToken);
                if (result.IsSuccess)
                    logger.LogInformation("Weekly batch: created={C} skipped={S} onHold={H}", result.Value.Created, result.Value.Skipped, result.Value.OnHold);
                else
                    logger.LogError("Weekly batch failed: {Code} {Message}", result.Error?.Code, result.Error?.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogError(ex, "PayoutBatchingService tick failed");
                FinanceDiagnostics.BgServiceFailures.Add(1, new("service", nameof(PayoutBatchingService)));
            }
        }
    }

    static DateTime NextWeekly(DateTime nowUtc, DayOfWeek targetDay, TimeOnly targetTime)
    {
        var diff = ((int)targetDay - (int)nowUtc.DayOfWeek + 7) % 7;
        var candidate = nowUtc.Date.AddDays(diff).Add(targetTime.ToTimeSpan());
        if (candidate <= nowUtc) candidate = candidate.AddDays(7);
        return candidate;
    }
}

public sealed class PayoutBatchingOptions
{
    public bool Enabled { get; set; } = true;
    public DayOfWeek TargetDayOfWeek { get; set; } = DayOfWeek.Sunday;
    public TimeOnly TargetTimeUtc { get; set; } = new(0, 0);  // midnight UTC
}
```

**Why we use MediatR.Send instead of duplicating handler code:** keeps the trigger flow DRY — admin manual trigger (T4) and BG service walk the same code path. Auditing identical.

**Edge cases:**
1. Service started Tuesday 14:00 → first run = next Sunday midnight (5 days away). Logged at startup so ops sees.
2. Multi-instance deployment → competing schedulers. Solution: use SQL-based distributed lock (`SELECT … FROM finance.LeaderElection WITH (UPDLOCK, READPAST) WHERE Key='PayoutBatching' AND ExpiresAt > now`). DEFERRED — single-instance acceptable v1. Document in 10-cross-cutting.md.
3. Tick takes > 1 hour → next tick still computes from current time, will still align with following Sunday.
4. Daylight saving / TZ confusion → never. Everything UTC.

---

## 2. RefundRetryService (every 15 min)

**Goal:** pick up Refund-type Payment rows with Status=Failed AND RetryCount<3 and retry the gateway call.

```csharp
internal sealed class RefundRetryService(
    IServiceProvider sp,
    ILogger<RefundRetryService> logger,
    TimeProvider tp,
    IOptions<RefundRetryOptions> opts)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opts.Value.Enabled) return;
        using var timer = new PeriodicTimer(opts.Value.Interval);
        await Task.Delay(opts.Value.InitialDelay, stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = sp.CreateScope();
            try { await ProcessBatchAsync(scope.ServiceProvider, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogError(ex, "RefundRetryService tick failed");
                FinanceDiagnostics.BgServiceFailures.Add(1, new("service", nameof(RefundRetryService)));
            }
        }
    }

    private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var paymentRepo = scoped.GetRequiredService<IPaymentRepository>();
        var gateway     = scoped.GetRequiredService<IPaymentGateway>();
        var uow         = scoped.GetRequiredService<IFinanceUnitOfWork>();

        var pending = await paymentRepo.GetPendingRefundsOlderThanAsync(
            cutoff: tp.GetUtcNow().UtcDateTime.AddMinutes(-opts.Value.RetryCooloffMinutes),
            maxRetries: opts.Value.MaxRetries,
            batchSize: opts.Value.BatchSize,
            ct);

        foreach (var refund in pending)
        {
            var original = await paymentRepo.GetByIdAsync(refund.OriginalPaymentId!.Value, ct);
            if (original is null) continue;

            try
            {
                var gw = await gateway.RefundAsync(
                    new RefundRequest(original.GatewayTransactionId, refund.AmountTotal.Amount, refund.AmountTotal.Currency, refund.RefundReason!.Value.ToString()),
                    ct);

                refund.IncrementRetryCount();
                if (gw.Status == RefundStatus.Completed) refund.MarkRefundCompleted(gw.GatewayRefundId, tp.GetUtcNow().UtcDateTime);
                else if (gw.Status == RefundStatus.Failed)
                {
                    refund.MarkRefundFailed(gw.FailureCode ?? "Unknown");
                    if (refund.RetryCount >= opts.Value.MaxRetries)
                    {
                        // Final failure — emit alert event
                        // RefundFailedDomainEvent → finance.refund.failed.v1 → Messaging admin alert
                        refund.RaiseRefundFinallyFailed();
                    }
                }
                // Pending stays Pending; webhook will catch up
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refund retry gateway error for {Id}", refund.Id);
                refund.IncrementRetryCount();
                refund.MarkRefundFailed("GatewayError");
            }
        }
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class RefundRetryOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(2);
    public int RetryCooloffMinutes { get; set; } = 5;  // don't retry the same row again within 5 min of last attempt
    public int MaxRetries { get; set; } = 3;
    public int BatchSize { get; set; } = 200;
}
```

**`Payment.IncrementRetryCount` / `RaiseRefundFinallyFailed` domain methods** added in this task.

**Inbox parity** — none needed. RefundRetryService is fully internal.

**Edge cases:**
1. Same refund row retried > cooloff window before next tick → cooloff guard prevents thrashing. Cutoff = "older than X minutes since LastModified".
2. Gateway slow (12 sec per call) × 200 batch = 40 min per tick → batch size smaller in prod (e.g. 50). Configurable.
3. After 3 failures + RefundFinallyFailed event → Messaging admin alert; ops manually reconciles via Stripe dashboard + admin endpoint POST /admin/payments/{id}/mark-refund-completed (deferred — manual SQL for v1).

---

## 3. DI Registration

```csharp
public static IServiceCollection AddFinanceInfrastructure(this IServiceCollection services, IConfiguration cfg)
{
    // ... DbContext, repos, UoW, inbox, outbox, gateway, ICommissionLookupService impl ...

    services.Configure<PayoutBatchingOptions>(cfg.GetSection("Finance:BackgroundServices:PayoutBatching"));
    services.Configure<RefundRetryOptions>(cfg.GetSection("Finance:BackgroundServices:RefundRetry"));
    services.AddHostedService<RefundRetryService>();
    services.AddHostedService<PayoutBatchingService>();
    return services;
}
```

`appsettings.json`:
```json
"Finance": {
  "BackgroundServices": {
    "PayoutBatching": { "Enabled": true, "TargetDayOfWeek": "Sunday", "TargetTimeUtc": "00:00:00" },
    "RefundRetry":    { "Enabled": true, "Interval": "00:15:00", "InitialDelay": "00:02:00", "RetryCooloffMinutes": 5, "MaxRetries": 3, "BatchSize": 200 }
  },
  "Payout": {
    "MinPayoutThreshold": { "JOD": 10, "USD": 15, "EUR": 12 },
    "LargePayoutThreshold": { "JOD": 5000, "USD": 7000, "EUR": 6500 },
    "EscrowReleaseDays": 7
  }
}
```

Dev overrides: `PayoutBatching.Interval = 00:10:00` (run every 10 min — fast feedback), `RefundRetry.RetryCooloffMinutes = 1`.

---

## 4. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Options classes + appsettings + dev overrides | 2 | 2026-10-06 |
| 2 | `PayoutBatchingService` impl + `NextWeekly` helper + unit test | 5 | 2026-10-07 |
| 3 | `RefundRetryService` impl + cooloff guard + unit test | 5 | 2026-10-07 |
| 4 | `Payment.IncrementRetryCount` + `RaiseRefundFinallyFailed` domain methods | 2 | 2026-10-08 |
| 5 | `FinanceDiagnostics` static class (ActivitySource + Meter + counters) | 2 | 2026-10-08 |
| 6 | DI registration + health check + boot log | 1 | 2026-10-08 |
| 7 | Integration test: completed payment + 7d escrow + run trigger → 1 Payout row | 3 | 2026-10-09 |
| 8 | Integration test: refund failed 3× → finally-failed event in outbox | 2 | 2026-10-10 |
| 9 | 24h soak test in pre-prod + verify OTEL counters | 3 | 2026-10-11 |
| 10 | PR review fixes | 3 | 2026-10-11 |
| **Total** | | **28h** | |

---

## 5. Acceptance gate for T5

- [ ] Boot log shows both services started with their schedule.
- [ ] OTEL counters incrementing: `bg_service_ticks_total{service=RefundRetryService}` ≈ 96 in 24h; `…{service=PayoutBatchingService}` = 0 or 1 (weekly).
- [ ] Smoke test: insert refund row with `Status=Failed, RetryCount=0, AmountTotal=10 JOD`, wait 15 min, query → `RetryCount=1, Status=Completed`.
- [ ] Smoke test (pre-prod): manually advance system date to Sunday 00:00 (or override `TargetTimeUtc` to next minute) → run service → assert 1 PayoutBatchCreated event in `finance.OutboxMessages`.
- [ ] Multi-instance test (if any): two API instances run; only ONE schedules a tick at midnight (lock contention — Tech Lead applies distributed lock if v2 sprint requires multi-instance).
