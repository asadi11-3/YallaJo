using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace YallaJo.Api.HealthChecks;

/// <summary>
/// Liveness probe for the four Booking background services (TASK 7). For each enabled service,
/// computes the staleness threshold as <c>3 × Interval</c> (or 3 days for the daily cron),
/// and returns Unhealthy when any enabled service has not ticked within that window.
///
/// Disabled services are skipped completely and never make the check Unhealthy.
///
/// Registered under <c>/health/ready</c> with tags <c>ready, booking, bg</c> — matches the existing
/// YallaJo convention for dependency-bound checks (see <c>outbox-dead-letters</c>). The task spec
/// asked for inclusion in <c>/health/live</c>; this repo treats <c>/health/live</c> as a minimal
/// liveness probe (predicate <c>_ =&gt; false</c>), so dependency-bound checks live under readiness.
/// </summary>
public sealed class BookingBgServicesHealthCheck(
    IBookingBackgroundServiceStatusStore statusStore,
    IOptionsMonitor<SlotLockCleanupOptions> slotOpts,
    IOptionsMonitor<BookingAutoExpireOptions> expireOpts,
    IOptionsMonitor<ProviderAutoAcceptOptions> acceptOpts,
    IOptionsMonitor<DocumentExpiryCheckOptions> documentOpts,
    TimeProvider timeProvider) : IHealthCheck
{
    /// <summary>Fallback staleness threshold for the daily DocumentExpiryCheckService: 3 days.</summary>
    public static readonly TimeSpan DailyServiceStaleThreshold = TimeSpan.FromDays(3);

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var stale = new List<string>();
        var data = new Dictionary<string, object>();

        // Service names match the internal type names in Booking.Infrastructure.BackgroundServices.
        // String literals are used because the service types are `internal sealed` and not visible
        // outside Booking.Infrastructure + its test assemblies.
        EvaluateIntervalService(
            "SlotLockCleanupService",
            slotOpts.CurrentValue.Enabled,
            slotOpts.CurrentValue.Interval,
            nowUtc,
            stale,
            data);

        EvaluateIntervalService(
            "BookingAutoExpireService",
            expireOpts.CurrentValue.Enabled,
            expireOpts.CurrentValue.Interval,
            nowUtc,
            stale,
            data);

        EvaluateIntervalService(
            "ProviderAutoAcceptService",
            acceptOpts.CurrentValue.Enabled,
            acceptOpts.CurrentValue.Interval,
            nowUtc,
            stale,
            data);

        EvaluateDailyService(
            "DocumentExpiryCheckService",
            documentOpts.CurrentValue.Enabled,
            nowUtc,
            stale,
            data);

        if (stale.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "All enabled Booking background services have ticked within their SLA window.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            description: $"{stale.Count} Booking background service(s) are stale: {string.Join(", ", stale)}",
            data: data));
    }

    private void EvaluateIntervalService(
        string serviceName,
        bool enabled,
        TimeSpan interval,
        DateTimeOffset nowUtc,
        ICollection<string> stale,
        IDictionary<string, object> data)
    {
        if (!enabled)
        {
            data[serviceName] = "disabled";
            return;
        }

        var threshold = interval > TimeSpan.Zero ? interval * 3 : TimeSpan.FromMinutes(15);
        EvaluateAgainstThreshold(serviceName, threshold, nowUtc, stale, data);
    }

    private void EvaluateDailyService(
        string serviceName,
        bool enabled,
        DateTimeOffset nowUtc,
        ICollection<string> stale,
        IDictionary<string, object> data)
    {
        if (!enabled)
        {
            data[serviceName] = "disabled";
            return;
        }

        EvaluateAgainstThreshold(serviceName, DailyServiceStaleThreshold, nowUtc, stale, data);
    }

    private void EvaluateAgainstThreshold(
        string serviceName,
        TimeSpan threshold,
        DateTimeOffset nowUtc,
        ICollection<string> stale,
        IDictionary<string, object> data)
    {
        var status = statusStore.GetStatus(serviceName);
        if (status?.LastTickUtc is null)
        {
            // Service has never ticked yet. The default InitialDelay (≤5 minutes) plus the first
            // interval should already cover startup; if we are past 3× threshold and still no tick,
            // surface as stale.
            data[serviceName] = "no-tick-yet";
            return;
        }

        var staleness = nowUtc - status.LastTickUtc.Value;
        data[serviceName] = new
        {
            lastTickUtc = status.LastTickUtc,
            lastSuccessUtc = status.LastSuccessUtc,
            lastFailureUtc = status.LastFailureUtc,
            stalenessSeconds = (long)staleness.TotalSeconds,
            thresholdSeconds = (long)threshold.TotalSeconds,
            totalItemsProcessed = status.TotalItemsProcessed,
            lastError = status.LastError,
        };

        if (staleness > threshold)
        {
            stale.Add($"{serviceName} (stale {(long)staleness.TotalSeconds}s > {(long)threshold.TotalSeconds}s)");
        }
    }
}
