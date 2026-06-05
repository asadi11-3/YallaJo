using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted <see cref="BackgroundService"/> that runs the Auth module's
/// retention cycle on a configurable interval. The actual work lives in
/// <see cref="IAuthRetentionWorker"/> so the cleanup logic is
/// unit-testable in isolation; this service owns the loop / scoping /
/// scheduling concerns only.
/// <para>
/// SC-2 hardening: the interval is now read from
/// <see cref="AuthRetentionOptions.RunIntervalHours"/> (was a hardcoded
/// 24h), and the loop honours <see cref="AuthRetentionOptions.Enabled"/>
/// so cleanup runs from a single host only (the API host enables it; the
/// Web / test hosts disable it).
/// </para>
/// </summary>
internal sealed class AuthCleanupService(
    IServiceProvider serviceProvider,
    IOptions<AuthRetentionOptions> options,
    ILogger<AuthCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    private readonly AuthRetentionOptions _opts = options.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_opts.Enabled)
        {
            logger.LogInformation(
                "AuthCleanupService disabled (Auth:Retention:Enabled = false); not running on this host.");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, _opts.RunIntervalHours));

        logger.LogInformation(
            "AuthCleanupService started (interval {IntervalHours}h).",
            interval.TotalHours);

        try { await Task.Delay(InitialDelay, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "AuthCleanupService: Unexpected error during cleanup cycle");
            }

            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }

        logger.LogInformation("AuthCleanupService stopped");
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var worker = scope.ServiceProvider.GetRequiredService<IAuthRetentionWorker>();

        var stopwatch = Stopwatch.StartNew();
        var outcome = await worker.ExecuteAsync(ct);
        stopwatch.Stop();

        logger.LogInformation(
            "AuthCleanupService: cycle complete in {DurationMs}ms — Otps={Otps}, Sessions={Sessions}, RefreshTokens={RefreshTokens}, ProcessedOutbox={Outbox}",
            stopwatch.ElapsedMilliseconds,
            outcome.OtpsDeleted,
            outcome.SessionsDeleted,
            outcome.RefreshTokensDeleted,
            outcome.ProcessedOutboxDeleted);
    }
}
