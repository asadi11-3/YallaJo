using Microsoft.Extensions.Diagnostics.HealthChecks;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.Api.HealthChecks;

/// <summary>
/// Returns Degraded if any module has dead-lettered outbox messages requiring manual intervention.
/// Tagged "outbox" + "ready" so it appears in /health/ready readiness checks.
/// </summary>
public sealed class OutboxDeadLetterHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var cleaners = scope.ServiceProvider.GetServices<IOutboxCleaner>();

        var data = new Dictionary<string, object>();
        int totalDeadLetters = 0;

        foreach (var cleaner in cleaners)
        {
            var count = await cleaner.CountDeadLetteredAsync(cancellationToken);
            if (count > 0)
                data[cleaner.ModuleName] = count;
            totalDeadLetters += count;
        }

        return totalDeadLetters == 0
            ? HealthCheckResult.Healthy("No dead-lettered outbox messages.")
            : HealthCheckResult.Degraded(
                $"{totalDeadLetters} dead-lettered outbox message(s) require manual intervention.",
                data: data);
    }
}
