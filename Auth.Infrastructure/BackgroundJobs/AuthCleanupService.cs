using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted <see cref="BackgroundService"/> that runs the Auth module's
/// retention cycle on a 24-hour interval. Phase 2C-4 refactored the
/// actual work into <see cref="IAuthRetentionWorker"/> so the cleanup
/// logic is unit-testable in isolation; this service owns the loop /
/// scoping / scheduling concerns only.
/// </summary>
internal sealed class AuthCleanupService(
    IServiceProvider serviceProvider,
    ILogger<AuthCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("AuthCleanupService started");

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

            try { await Task.Delay(Interval, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }

        logger.LogInformation("AuthCleanupService stopped");
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var worker = scope.ServiceProvider.GetRequiredService<IAuthRetentionWorker>();

        var outcome = await worker.ExecuteAsync(ct);

        logger.LogInformation(
            "AuthCleanupService: cycle complete — Otps={Otps}, Sessions={Sessions}, RefreshTokens={RefreshTokens}, ProcessedOutbox={Outbox}",
            outcome.OtpsDeleted,
            outcome.SessionsDeleted,
            outcome.RefreshTokensDeleted,
            outcome.ProcessedOutboxDeleted);
    }
}
