using System.Diagnostics;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

/// <summary>
/// Singleton <see cref="ActivitySource"/> for outbox dispatch spans.
/// Name: <c>YallaJo.Outbox</c> — register in OTel config via <c>.AddSource("YallaJo.Outbox")</c>
/// (already done in <c>OpenTelemetryExtensions.cs</c>).
/// </summary>
internal static class OutboxActivitySource
{
    public static readonly ActivitySource Instance = new("YallaJo.Outbox", "1.0.0");
}
