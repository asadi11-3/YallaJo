namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Persistent daily budget gate for weather API calls.
/// PDF §11: max 1000 calls/day; must survive restarts and multi-instance deployments.
/// </summary>
public interface IWeatherBudgetGate
{
    /// <summary>
    /// Attempts to consume one call from today's budget.
    /// Returns true if the call is within budget; false if exhausted.
    /// Also emits a WeatherBudgetExhaustedIntegrationEvent once per day when budget is first exhausted.
    /// </summary>
    Task<bool> TryConsumeAsync(CancellationToken ct = default);
}
