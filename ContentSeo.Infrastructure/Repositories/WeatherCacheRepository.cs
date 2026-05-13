using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWeatherCacheRepository"/>.
/// Compile-only stub for Wave-4 pre-work — by-place / expiry-cleanup overrides
/// will be added during TASK 4 implementation.
/// </summary>
internal sealed class WeatherCacheRepository(ContentSeoDbContext context)
    : EfRepository<WeatherCache, Guid>(context), IWeatherCacheRepository;
