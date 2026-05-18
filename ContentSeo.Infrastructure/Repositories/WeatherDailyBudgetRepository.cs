using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Repositories;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWeatherDailyBudgetRepository"/>.
/// </summary>
internal sealed class WeatherDailyBudgetRepository(ContentSeoDbContext context)
    : EfRepository<WeatherDailyBudget, Guid>(context), IWeatherDailyBudgetRepository
{
    /// <inheritdoc/>
    public async Task<WeatherDailyBudget?> GetByDateAsync(DateOnly date, CancellationToken ct = default)
        => await GetAsync(b => b.Date == date, asNoTracking: false, ct: ct);
}
