using System.Diagnostics.Metrics;

namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// OpenTelemetry metrics for the outbox subsystem.
/// Meter name: <c>YallaJo.Outbox</c> — register in OTel config via <c>.AddMeter("YallaJo.Outbox")</c>.
/// </summary>
public static class OutboxMetrics
{
    public static readonly Meter Meter = new("YallaJo.Outbox", "1.0.0");

    /// <summary>Total outbox messages successfully dispatched (tag: module).</summary>
    public static readonly Counter<long> ProcessedTotal =
        Meter.CreateCounter<long>(
            "outbox.processed.total",
            description: "Total outbox messages successfully dispatched");

    /// <summary>Total outbox messages that failed dispatch and will retry (tag: module).</summary>
    public static readonly Counter<long> FailedTotal =
        Meter.CreateCounter<long>(
            "outbox.failed.total",
            description: "Total outbox messages that failed dispatch (will retry)");

    /// <summary>Total outbox messages moved to dead-letter (tags: module, type).</summary>
    public static readonly Counter<long> DeadLetteredTotal =
        Meter.CreateCounter<long>(
            "outbox.dead_lettered.total",
            description: "Total outbox messages moved to dead-letter (RetryCount >= max)");

    /// <summary>Time from message OccurredOnUtc to successful dispatch (ms).</summary>
    public static readonly Histogram<double> DispatchLatencyMs =
        Meter.CreateHistogram<double>(
            "outbox.dispatch.latency_ms",
            description: "Time from message creation to successful dispatch (ms)");

    /// <summary>RetryCount at the moment of success — histogram of retry distribution.</summary>
    public static readonly Histogram<int> RetryCountDistribution =
        Meter.CreateHistogram<int>(
            "outbox.retry.count",
            description: "RetryCount at the moment of success");

    /// <summary>Per-handler success count (tag: handler).</summary>
    public static readonly Counter<long> HandlerSuccessTotal =
        Meter.CreateCounter<long>(
            "outbox.handler.success.total",
            description: "Per-handler success count");

    /// <summary>Per-handler failure count (tag: handler).</summary>
    public static readonly Counter<long> HandlerFailureTotal =
        Meter.CreateCounter<long>(
            "outbox.handler.failure.total",
            description: "Per-handler failure count");

    /// <summary>Total rows deleted by OutboxCleanupBackgroundService.</summary>
    public static readonly Counter<long> CleanupDeletedTotal =
        Meter.CreateCounter<long>(
            "outbox.cleanup.deleted.total",
            description: "Total rows deleted by OutboxCleanupBackgroundService");
}
