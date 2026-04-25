using System.Diagnostics;
using System.Text.Json;

namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Captures and restores W3C trace context (traceparent + tracestate) across
/// the async outbox boundary using only BCL types (no OpenTelemetry.Api dependency).
///
/// <b>Capture</b>: called when writing to the outbox — stores the current
/// <see cref="Activity.Id"/> (W3C traceparent) and <see cref="Activity.TraceStateString"/>
/// as a compact JSON string in <c>OutboxMessage.TraceContext</c>.
///
/// <b>TryRestoreContext</b>: called by the outbox processor before dispatching each
/// message — parses the stored string back to an <see cref="ActivityContext"/> so the
/// dispatch span becomes a child of the original HTTP request span.
/// </summary>
public static class TraceContextHelpers
{
    private sealed record TraceContextPayload(string? Traceparent, string? Tracestate);

    private static readonly JsonSerializerOptions SerializerOptions =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Captures the current W3C trace context from <see cref="Activity.Current"/>.
    /// Returns <c>null</c> when there is no active span (e.g. background startup tasks).
    /// </summary>
    public static string? Capture()
    {
        var activity = Activity.Current;
        var traceparent = activity?.Id;

        if (traceparent is null) return null;

        var payload = new TraceContextPayload(traceparent, activity!.TraceStateString);
        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Tries to parse a stored trace context string back to an <see cref="ActivityContext"/>.
    /// Returns <c>null</c> on empty input or parse failure — callers should start a
    /// root span in that case.
    /// </summary>
    public static ActivityContext? TryRestoreContext(string? serialized)
    {
        if (string.IsNullOrEmpty(serialized)) return null;

        try
        {
            var payload = JsonSerializer.Deserialize<TraceContextPayload>(serialized, SerializerOptions);
            if (payload?.Traceparent is null) return null;

            if (ActivityContext.TryParse(payload.Traceparent, payload.Tracestate, isRemote: true, out var ctx))
                return ctx;
        }
        catch
        {
            // Malformed payload — silently ignore, caller starts a fresh root span.
        }

        return null;
    }
}
